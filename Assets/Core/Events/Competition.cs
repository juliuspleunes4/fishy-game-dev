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
        public Guid CompetitionId { get; set; } // Backend competition UUID
            
        public Competition(ICompetitionState competitionState, DateTime start, DateTime end, StoreManager.CurrencyType rewardCurrency, List<int> prizepool)
        {
            _competitionState = competitionState;
            _startDateTime = start;
            _endDateTime = end;
            _rewardCurrency = rewardCurrency;
            _prizepool = prizepool;
            CompetitionId = Guid.Empty; // Will be set from backend
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
                
                // Sync score to backend
                SyncScoreToBackend(playerId, playerName, newResult);
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
                
                // Sync score to backend
                SyncScoreToBackend(playerId, playerName, newResult);
            }
        }

        private void SyncScoreToBackend(Guid playerId, string playerName, int score)
        {
            if (RunningCompetition.CompetitionId == Guid.Empty)
            {
                Debug.LogWarning("[CompetitionManager] Cannot sync score - competition has no backend ID");
                return;
            }
            
            DatabaseCommunications.UpdateCompetitionScore(
                RunningCompetition.CompetitionId,
                playerId,
                playerName,
                score
            );
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

        // >> Polls the backend server for active and upcoming competitions.
        // >> Syncs local competition state with the database every 60 seconds.
        [Server]
        public static IEnumerator AutoGenerateEvents()
        {
            // Wait a bit on startup to ensure all systems are initialized
            yield return new WaitForSeconds(10f);
            
            Debug.Log("[CompetitionManager] Backend sync system started");
            
            // Initial sync
            FetchCompetitionsFromBackend();
            
            while (true)
            {
                // Poll backend every 60 seconds
                yield return new WaitForSeconds(60f);
                FetchCompetitionsFromBackend();
            }
        }

        [Server]
        private static void FetchCompetitionsFromBackend()
        {
            // Fetch active competitions
            DatabaseCommunications.GetActiveCompetitions((response) =>
            {
                if (response.EndRequestReason == WebRequestHandler.RequestEndReason.success)
                {
                    try
                    {
                        CompetitionWithLeaderboardResponse[] activeCompetitions = 
                            JsonUtility.FromJson<CompetitionWithLeaderboardWrapper>("{\"competitions\":" + response.ResponseData + "}").competitions;
                        
                        foreach (var apiComp in activeCompetitions)
                        {
                            Guid compId = Guid.Parse(apiComp.competition_id);
                            
                            // Check if we already have this competition active
                            if (_currentCompetition != null && _currentCompetition.CompetitionData.RunningCompetition.CompetitionId == compId)
                            {
                                // Update leaderboard
                                UpdateLeaderboardFromAPI(apiComp.leaderboard);
                                continue;
                            }
                            
                            // Start this competition if it's not active
                            Competition competition = ConvertAPIToCompetition(apiComp);
                            SetCurrentCompetition(competition);
                            UpdateLeaderboardFromAPI(apiComp.leaderboard);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[CompetitionManager] Failed to parse active competitions: {e.Message}");
                    }
                }
            });
            
            // Fetch upcoming competitions
            DatabaseCommunications.GetUpcomingCompetitions((response) =>
            {
                if (response.EndRequestReason == WebRequestHandler.RequestEndReason.success)
                {
                    try
                    {
                        CompetitionResponse[] upcomingCompetitions = 
                            JsonUtility.FromJson<CompetitionArrayWrapper>("{\"competitions\":" + response.ResponseData + "}").competitions;
                        
                        // Clear and rebuild upcoming list
                        _upcomingCompetitions.Clear();
                        
                        foreach (var apiComp in upcomingCompetitions)
                        {
                            Competition competition = ConvertAPIToCompetition(apiComp);
                            _upcomingCompetitions.Add(competition);
                        }
                        
                        Debug.Log($"[CompetitionManager] Synced {_upcomingCompetitions.Count} upcoming competitions from backend");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[CompetitionManager] Failed to parse upcoming competitions: {e.Message}");
                    }
                }
            });
        }

        [Server]
        private static Competition ConvertAPIToCompetition(CompetitionResponse apiComp)
        {
            ICompetitionState state = apiComp.competition_type switch
            {
                1 => new MostFishCompetitonState { specificFish = true, fishIDToCatch = apiComp.target_fish_id },
                2 => new MostItemsCompetitonState { ItemId = apiComp.target_fish_id },
                3 => new largestFishCompetitonState { specificFish = true, fishIDToCatch = apiComp.target_fish_id },
                _ => throw new NotSupportedException($"Unknown competition type: {apiComp.competition_type}")
            };
            
            StoreManager.CurrencyType currency = apiComp.reward_currency == "coins" 
                ? StoreManager.CurrencyType.coins 
                : StoreManager.CurrencyType.bucks;
            
            List<int> prizes = new List<int>
            {
                apiComp.prize_1st, apiComp.prize_2nd, apiComp.prize_3rd, apiComp.prize_4th, apiComp.prize_5th,
                apiComp.prize_6th, apiComp.prize_7th, apiComp.prize_8th, apiComp.prize_9th, apiComp.prize_10th
            };
            
            DateTime startTime = DateTime.Parse(apiComp.start_time).ToUniversalTime();
            DateTime endTime = DateTime.Parse(apiComp.end_time).ToUniversalTime();
            
            Competition competition = new Competition(state, startTime, endTime, currency, prizes)
            {
                CompetitionId = Guid.Parse(apiComp.competition_id)
            };
            
            return competition;
        }

        [Server]
        private static Competition ConvertAPIToCompetition(CompetitionWithLeaderboardResponse apiComp)
        {
            CompetitionResponse baseComp = new CompetitionResponse
            {
                competition_id = apiComp.competition_id,
                competition_type = apiComp.competition_type,
                target_fish_id = apiComp.target_fish_id,
                start_time = apiComp.start_time,
                end_time = apiComp.end_time,
                reward_currency = apiComp.reward_currency,
                prize_1st = apiComp.prize_1st,
                prize_2nd = apiComp.prize_2nd,
                prize_3rd = apiComp.prize_3rd,
                prize_4th = apiComp.prize_4th,
                prize_5th = apiComp.prize_5th,
                prize_6th = apiComp.prize_6th,
                prize_7th = apiComp.prize_7th,
                prize_8th = apiComp.prize_8th,
                prize_9th = apiComp.prize_9th,
                prize_10th = apiComp.prize_10th,
                created_at = apiComp.created_at
            };
            
            return ConvertAPIToCompetition(baseComp);
        }

        [Server]
        private static void UpdateLeaderboardFromAPI(CompetitionParticipantResponse[] leaderboard)
        {
            if (_currentCompetition == null) return;
            
            // Update current competition leaderboard with backend data
            foreach (var participant in leaderboard)
            {
                Guid playerId = Guid.Parse(participant.user_id);
                _currentCompetition.CompetitionData.AddOrUpdateResult(playerId, participant.user_name, participant.score);
            }
        }

        // JSON wrapper classes for array deserialization
        [Serializable]
        private class CompetitionArrayWrapper
        {
            public CompetitionResponse[] competitions;
        }

        [Serializable]
        private class CompetitionWithLeaderboardWrapper
        {
            public CompetitionWithLeaderboardResponse[] competitions;
        }

        // >> DEPRECATED: Local event generation replaced with backend sync
        // >> Kept for reference but no longer called
        [Server]
        private static void CreateRandomEvent()
        {
            Debug.LogWarning("[CompetitionManager] CreateRandomEvent() is deprecated. Events are now generated by the backend.");
        }

        [Server]
        private static List<int> GeneratePrizeDistribution(StoreManager.CurrencyType currency)
        {
            Debug.LogWarning("[CompetitionManager] GeneratePrizeDistribution() is deprecated. Prize distribution is managed by the backend.");
            return new List<int>();
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
