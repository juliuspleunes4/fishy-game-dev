using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GlobalCompetitionSystem;
using Mirror;
using NUnit.Framework;
using UnityEngine;

namespace GlobalCompetitionSystem
{
    public class Competition
    {
        private readonly ICompetitionState _competitionState;
        private readonly DateTime _startDateTime;
        private readonly DateTime _endDateTime;
        private StoreManager.CurrencyType _rewardCurrency;
        // index 0 is the prize for first place, etc...
        private List<int> _prizepool;
        public ICompetitionState CompetitionState => _competitionState;
        public DateTime StartDateTime => _startDateTime;
        public DateTime EndDateTime => _endDateTime;
        public StoreManager.CurrencyType RewardCurrency => _rewardCurrency;
        public List<int> Prizepool => _prizepool;
            
        public Competition(ICompetitionState competitionState, DateTime start, DateTime end, StoreManager.CurrencyType rewardCurrency, List<int> prizepool)
        {
            _competitionState = competitionState;
            _startDateTime = start;
            _endDateTime = end;
            _rewardCurrency = rewardCurrency;
            _prizepool = prizepool;
        }
    }
    
    public struct CurrentCompetitionData
    {
        public Competition RunningCompetition { get; }
        // Updated once in a while, might take quite a bit of server load when the rankings contain a lot of players, so this is most of the time not up to date
        private readonly Dictionary<Guid, int> _playerRanking;
        // int -> score, List<PlayerResult> -> all players with that score
        private readonly SortedDictionary<int, List<PlayerResult>> _results;
        private readonly Dictionary<Guid, int> _playerScoreLookup;


        public CurrentCompetitionData(Competition runningCompetition)
        {
            RunningCompetition = runningCompetition;
            _playerRanking = new Dictionary<Guid, int>(100);
            _results = new SortedDictionary<int, List<PlayerResult>>();
            _playerScoreLookup = new Dictionary<Guid, int>(100);
        }

        public void UpdatePlayerRankings()
        {
            _playerRanking.Clear();
            int currentRank = 1;
            foreach (var scoreGroup in _results.Reverse())
            {
                foreach (var result in scoreGroup.Value)
                {
                    _playerRanking[result.PlayerID] = currentRank;
                    currentRank++;
                }
            }
        }

        public void AddOrUpdateResult(Guid playerId, string playerName, int newResult)
        {
            if (_playerScoreLookup.TryGetValue(playerId, out int oldResult))
            {
                List<PlayerResult> playersAtOldScore = _results[oldResult];
                PlayerResult playerObject = playersAtOldScore.First(p => p.PlayerID == playerId);

                // Update the position in Results first
                if (oldResult != newResult)
                {
                    playersAtOldScore.Remove(playerObject);
                    if (playersAtOldScore.Count == 0)
                    {
                        _results.Remove(oldResult);
                    }

                    if (!_results.ContainsKey(newResult))
                    {
                        _results[newResult] = new List<PlayerResult>();
                    }
                    _results[newResult].Add(playerObject);
                }
                
                // Then update the class itself
                playerObject.PlayerName = playerName;
                playerObject.Result = newResult;
                _playerScoreLookup[playerId] = newResult;
            }
            else
            {
                if (!_results.ContainsKey(newResult))
                {
                    _results[newResult] = new List<PlayerResult>();
                }
            
                PlayerResult newPlayerResult = new PlayerResult(playerId, playerName, newResult);
                _results[newResult].Add(newPlayerResult);
                _playerScoreLookup[playerId] = newResult;
            }
        }

        public SortedList<int, PlayerResult> GetTopPerformers(int amount)
        {
            var topPlayers = new SortedList<int, PlayerResult>(amount);

            foreach (var (player, index) in _results
                         .Reverse()
                         .SelectMany(kvp => kvp.Value)
                         .Take(amount)
                         .Select((player, index) => (player, index)))
            {
                topPlayers.Add(index + 1, player);
            }

            return topPlayers;
        }


        public (int, PlayerResult) GetPlayerResult(Guid playerID)
        {
            if (_playerScoreLookup.TryGetValue(playerID, out int score))
            {
                if (_playerRanking.TryGetValue(playerID, out int rank))
                {
                    return (rank, _results[score].First(r => r.PlayerID == playerID));
                }
            }
            return (0, null);
        }
    }
    
    public class PlayerResult
    {
        private readonly Guid _playerID;
        public string PlayerName;
        public int Result;

        public Guid PlayerID => _playerID;
            
        public PlayerResult(Guid playerID, string playerName, int result)
        {
            _playerID = playerID;
            PlayerName = playerName;
            Result = result;
        }
    }
    
    class CompetitionStartDateComparer : IComparer<Competition>
    {
        public int Compare(Competition x, Competition y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;
            return x.StartDateTime.CompareTo(y.StartDateTime);
        }
    }

    public static class CompetitionManager
    {
        [SyncVar] private static CurrentCompetition _currentCompetition;
        private static readonly SyncSortedSet<Competition> _upcomingCompetitions = new SyncSortedSet<Competition>(new CompetitionStartDateComparer());

        public static CurrentCompetition GetCurrentCompetition()
        {
            return _currentCompetition;
        }

        public static SyncSortedSet<Competition> GetUpcomingCompetitions()
        {
            return _upcomingCompetitions;
        }

        [Server]
        public static IEnumerator UpdateCompetitions()
        {
            DateTime lastRankingRefresh = DateTime.MinValue;
            // hours, minute, seconds
            TimeSpan timeBetweenRankingRebuilds = new TimeSpan(0, 1, 0);
            while (true)
            {
                if (_currentCompetition == null)
                {
                    if (_upcomingCompetitions.Count > 0)
                    {
                        Competition nextCompetition = _upcomingCompetitions.First();
                        if (nextCompetition.StartDateTime >= DateTime.Now)
                        {
                            SetCurrentCompetition(nextCompetition);
                            _upcomingCompetitions.Remove(nextCompetition);
                        }
                    }
                }
                
                if (_currentCompetition != null && _currentCompetition.CompetitionData.RunningCompetition.EndDateTime < DateTime.UtcNow)
                {
                    EndCurrentCompetition();
                    _currentCompetition = null;
                }

                if (_currentCompetition != null && DateTime.Now - lastRankingRefresh > timeBetweenRankingRebuilds)
                {
                    _currentCompetition.CompetitionData.UpdatePlayerRankings();
                    lastRankingRefresh = DateTime.Now;
                }
                yield return new WaitForSeconds(1);
            }
        }

        // >> Automatically generates and schedules new competitions to keep the event queue populated.
        // >> Checks daily and maintains a minimum of 3 upcoming events.
        [Server]
        public static IEnumerator AutoGenerateEvents()
        {
            // Wait a bit on startup to ensure all systems are initialized
            yield return new WaitForSeconds(10f);
            
            DateTime lastEventCheck = DateTime.MinValue;
            TimeSpan checkInterval = new TimeSpan(24, 0, 0); // Check once per day
            int minUpcomingEvents = 3;
            
            Debug.Log("[CompetitionManager] Auto-generation system started");
            
            while (true)
            {
                // Check if we need to generate new events
                if (DateTime.Now - lastEventCheck > checkInterval)
                {
                    lastEventCheck = DateTime.Now;
                    
                    if (_upcomingCompetitions.Count < minUpcomingEvents)
                    {
                        int eventsToGenerate = minUpcomingEvents - _upcomingCompetitions.Count;
                        Debug.Log($"[CompetitionManager] Generating {eventsToGenerate} new events. Current count: {_upcomingCompetitions.Count}");
                        
                        for (int i = 0; i < eventsToGenerate; i++)
                        {
                            CreateRandomEvent();
                        }
                    }
                }
                
                // Check every hour
                yield return new WaitForSeconds(3600f);
            }
        }

        // >> Creates a random competition event with varied parameters.
        // >> Currently supports fish collection competitions with randomized rewards and durations.
        [Server]
        private static void CreateRandomEvent()
        {
            // Find the latest upcoming event to schedule after it
            DateTime startDate;
            if (_upcomingCompetitions.Count > 0)
            {
                Competition lastEvent = _upcomingCompetitions.Last();
                // Schedule 6-12 hours after the last event ends
                int hoursDelay = UnityEngine.Random.Range(6, 13);
                startDate = lastEvent.EndDateTime.AddHours(hoursDelay);
            }
            else if (_currentCompetition != null)
            {
                // Schedule after current competition
                int hoursDelay = UnityEngine.Random.Range(6, 13);
                startDate = _currentCompetition.CompetitionData.RunningCompetition.EndDateTime.AddHours(hoursDelay);
            }
            else
            {
                // No competitions at all, start soon
                startDate = DateTime.Now.AddHours(UnityEngine.Random.Range(2, 6));
            }
            
            // Event duration: 12-48 hours
            int durationHours = UnityEngine.Random.Range(12, 49);
            DateTime endDate = startDate.AddHours(durationHours);
            
            // Select a random fish to collect
            ItemDefinition[] allItems = ItemSystem.ItemRegistry.GetFullItemsList();
            List<ItemDefinition> fishItems = new List<ItemDefinition>();
            
            foreach (var item in allItems)
            {
                var fishBehaviour = item.GetBehaviour<ItemSystem.FishBehaviour>();
                if (fishBehaviour != null)
                {
                    fishItems.Add(item);
                }
            }
            
            if (fishItems.Count == 0)
            {
                Debug.LogWarning("[CompetitionManager] No fish items found for event generation!");
                return;
            }
            
            ItemDefinition selectedFish = fishItems[UnityEngine.Random.Range(0, fishItems.Count)];
            
            // Create competition state
            MostItemsCompetitonState state = new MostItemsCompetitonState
            {
                ItemId = selectedFish.Id
            };
            
            // Randomize currency type (70% coins, 30% bucks for rarer rewards)
            StoreManager.CurrencyType currency = UnityEngine.Random.value < 0.7f 
                ? StoreManager.CurrencyType.coins 
                : StoreManager.CurrencyType.bucks;
            
            // Generate prize distribution based on currency type
            List<int> prizeDistribution = GeneratePrizeDistribution(currency);
            
            Debug.Log($"[CompetitionManager] Created event: Collect {selectedFish.DisplayName} | Start: {startDate:yyyy-MM-dd HH:mm} | Duration: {durationHours}h | Currency: {currency}");
            
            AddUpcomingCompetition(state, startDate, endDate, currency, prizeDistribution);
        }

        /// <summary>
        /// Generates a prize distribution for the top 10 players.
        /// Bucks have lower amounts since they're more valuable.
        /// </summary>
        [Server]
        private static List<int> GeneratePrizeDistribution(StoreManager.CurrencyType currency)
        {
            List<int> prizes = new List<int>(10);
            
            if (currency == StoreManager.CurrencyType.coins)
            {
                // Coins: More generous amounts
                prizes.Add(UnityEngine.Random.Range(800, 1201));  // 1st place: 800-1200
                prizes.Add(UnityEngine.Random.Range(500, 801));   // 2nd place: 500-800
                prizes.Add(UnityEngine.Random.Range(300, 501));   // 3rd place: 300-500
                prizes.Add(UnityEngine.Random.Range(200, 301));   // 4th place: 200-300
                prizes.Add(UnityEngine.Random.Range(150, 201));   // 5th place: 150-200
                prizes.Add(UnityEngine.Random.Range(100, 151));   // 6th place: 100-150
                prizes.Add(UnityEngine.Random.Range(75, 101));    // 7th place: 75-100
                prizes.Add(UnityEngine.Random.Range(50, 76));     // 8th place: 50-75
                prizes.Add(UnityEngine.Random.Range(30, 51));     // 9th place: 30-50
                prizes.Add(UnityEngine.Random.Range(20, 31));     // 10th place: 20-30
            }
            else // Bucks
            {
                // Bucks: More conservative amounts (premium currency)
                prizes.Add(UnityEngine.Random.Range(80, 121));    // 1st place: 80-120
                prizes.Add(UnityEngine.Random.Range(50, 81));     // 2nd place: 50-80
                prizes.Add(UnityEngine.Random.Range(30, 51));     // 3rd place: 30-50
                prizes.Add(UnityEngine.Random.Range(20, 31));     // 4th place: 20-30
                prizes.Add(UnityEngine.Random.Range(15, 21));     // 5th place: 15-20
                prizes.Add(UnityEngine.Random.Range(10, 16));     // 6th place: 10-15
                prizes.Add(UnityEngine.Random.Range(8, 11));      // 7th place: 8-10
                prizes.Add(UnityEngine.Random.Range(6, 9));       // 8th place: 6-8
                prizes.Add(UnityEngine.Random.Range(4, 7));       // 9th place: 4-6
                prizes.Add(UnityEngine.Random.Range(2, 5));       // 10th place: 2-4
            }
            
            return prizes;
        }

        [Server]
        public static void AddUpcomingCompetition(ICompetitionState competitionState, DateTime startDate,
            DateTime endDate, StoreManager.CurrencyType rewardCurrency, List<int> rewardDistribution)
        {
            _upcomingCompetitions.Add(new Competition(competitionState, startDate, endDate, rewardCurrency,
                rewardDistribution));
        }

        [Server]
        private static void SetCurrentCompetition(Competition metadata)
        {
            CurrentCompetition currentCompetition =
                CompetitionStateRegistry.GetImplementation(metadata.CompetitionState);
            currentCompetition.SetState(metadata.CompetitionState);
            currentCompetition.CompetitionData = new CurrentCompetitionData(metadata);
            _currentCompetition = currentCompetition;
        }

        [Server]
        private static void EndCurrentCompetition()
        {
            DistributePrizes();
            MailResults();
        }

        [Server]
        private static void DistributePrizes()
        {
            List<int> prizes = _currentCompetition.CompetitionData.RunningCompetition.Prizepool;
            SortedList<int, PlayerResult> winners = _currentCompetition.CompetitionData.GetTopPerformers(prizes.Count);
            for (int i = 0; i < winners.Count; i++)
            {
                PlayerResult winner = winners[i];
                bool prizeGiven = false;
                
                if (GameNetworkManager.connUUID.TryGetValue(winner.PlayerID, out NetworkConnectionToClient playerConnection))
                {
                    PlayerDataSyncManager syncManager = playerConnection.identity.GetComponent<PlayerDataSyncManager>();
                    if (syncManager != null)
                    {
                        switch (_currentCompetition.CompetitionData.RunningCompetition.RewardCurrency)
                        {
                            case StoreManager.CurrencyType.bucks:
                                syncManager.ChangeFishBucksAmount(prizes[i], true);
                                break;
                            case StoreManager.CurrencyType.coins:
                                syncManager.ChangeFishCoinsAmount(prizes[i], true);
                                break;
                            default:
                                throw new NotSupportedException($"Currency type {_currentCompetition.CompetitionData.RunningCompetition.RewardCurrency} has not yet been implemented as a reward");
                        }
                        prizeGiven = true;
                    }
                }
                
                if(!prizeGiven)
                {
                    switch (_currentCompetition.CompetitionData.RunningCompetition.RewardCurrency)
                    {
                        case StoreManager.CurrencyType.bucks:
                            DatabaseCommunications.ChangeFishBucksAmount(prizes[i], winner.PlayerID);
                            break;
                        case StoreManager.CurrencyType.coins:
                            DatabaseCommunications.ChangeFishCoinsAmount(prizes[i], winner.PlayerID);
                            break;
                        default:
                            throw new NotSupportedException($"Currency type {_currentCompetition.CompetitionData.RunningCompetition.RewardCurrency} has not yet been implemented as a reward");
                    }
                }
            }
        }

        [Server]
        public static void MailResults()
        {
         throw new NotImplementedException();   
        }

        public static bool AddToRunningCompetition<T>(T data, PlayerData playerData)
        {
            if (_currentCompetition is CurrentCompetition<T> competition)
            {
                return competition.AddToCompetition(data, playerData);
            }

            return false;
        }
    }

    // Non-generic interfaces for type erasure
    public abstract class CurrentCompetition
    {
        public CurrentCompetitionData CompetitionData { get; set; }
        public abstract void SetState(ICompetitionState state);
    }

    public abstract class CurrentCompetition<T> : CurrentCompetition
    {
        ICompetitionState State { get; set; }
        public abstract bool AddToCompetition(T data, PlayerData playerData);
    }
}

public static class PlayerResultReaderWriter
{
    public static void WritePlayerResult(this NetworkWriter writer, PlayerResult result)
    {
        writer.WriteGuid(result.PlayerID);
        writer.WriteString(result.PlayerName);
        writer.WriteInt(result.Result);
    }

    public static PlayerResult ReadPlayerResult(this NetworkReader reader)
    {
        return new PlayerResult(
            reader.ReadGuid(),
            reader.ReadString(),
            reader.ReadInt()
            );
    }
}
