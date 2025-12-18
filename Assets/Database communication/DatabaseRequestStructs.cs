using System;

#nullable enable
// Authenticate requests
[Serializable]
public class LoginRequest
{
    public string username;
    public string password;
}

// player data requests
[Serializable]
public class RetreiveDataRequest
{
    public string user_id;
}

// Inventory requests
[Serializable]
public class AddItemRequest
{
    public string user_id;
    public int item_id;
    public string item_uid;
    public int amount;
    public int cell_id;
}

[Serializable]
public class AddOrUpdateItemRequest {
    public string user_id;
    public string item_uuid;
    public int definition_id;
    public string state_blob; // base64
}

[Serializable]
public class DegradeItemRequest
{
    public string user_id;
    public string item_uid;
    public int amount;
}

[Serializable]
public class IncreaseItemRequest
{
    public string user_id;
    public string item_uid;
    public int amount;
}

[Serializable]
public class DestroyItemRequest
{
    public string user_id;
    public string item_uid;
}

// Mail requests
[Serializable]
public class CreateMailRequest
{
    public string mail_id;
    public string sender_id;
    public string[] receiver_ids;
    public string title;
    public string message;
}

[Serializable]
public class DeleteMailRequest
{
    public string user_id;
    public string mail_id;
}

[Serializable]
public class ReadMailRequest
{
    public string user_id;
    public string mail_id;
    public bool read;
}

[Serializable]
public class ArchiveMailRequest
{
    public string user_id;
    public string mail_id;
    public bool archived;
}


// Change stats requests
[Serializable]
public class SelectItemRequest
{
    public string user_id;
    public string item_uid;
    public string item_type;
}
[Serializable]
public class AddXPRequest
{
    public string user_id;
    public int amount;
}

[Serializable]
public class ChangeBucksRequest
{
    public string user_id;
    public int amount;
}

[Serializable]
public class ChangeCoinsRequest
{
    public string user_id;
    public int amount;
}

[Serializable]
public class AddPlayTimeRequest
{
    public string user_id;
    public int amount;
}

[Serializable]
public class AddFishRequest
{
    public string user_id;
    public int length;
    public int fish_id;
    public int bait_id;
    public int area_id;
}

[Serializable]
public class CreateFriendRequest
{
    public string user_one;
    public string user_two;
    public string sender_id;
}

[Serializable]
public class HandleFriendRequest
{
    public string user_one;
    public string user_two;
    public bool request_accepted;
}

[Serializable]
public class RemoveFriendRequest
{
    public string user_one;
    public string user_two;
}

// user requests
[Serializable]
public class CreateUserRequest
{
    public string email;
    public string username;
    public string password;
}

// Active Effects requests
[Serializable]
public class AddActiveEffectRequest
{
    public string user_id;
    public int item_id;          // ItemDefinition ID that created this effect
    public string expiry_time;   // DateTime as ISO 8601 string
}

[Serializable]
public class RemoveActiveEffectRequest
{
    public string user_id;
    public int item_id;          // ItemDefinition ID to identify which effect to remove
}

[Serializable]
public class RemoveExpiredEffectsRequest
{
    public string user_id;
    public int item_id;
}

// Competition requests and responses
[Serializable]
public class CompetitionResponse
{
    public string competition_id;
    public int competition_type; // 1=MostFish, 2=MostItems, 3=LargestFish
    public int target_fish_id;
    public string start_time; // ISO 8601 UTC
    public string end_time; // ISO 8601 UTC
    public string reward_currency; // "coins" or "bucks"
    public int prize_1st;
    public int prize_2nd;
    public int prize_3rd;
    public int prize_4th;
    public int prize_5th;
    public int prize_6th;
    public int prize_7th;
    public int prize_8th;
    public int prize_9th;
    public int prize_10th;
    public string created_at;
}

[Serializable]
public class CompetitionParticipantResponse
{
    public string competition_id;
    public string user_id;
    public string user_name;
    public int score;
    public string last_updated;
}

[Serializable]
public class CompetitionWithLeaderboardResponse
{
    public string competition_id;
    public int competition_type;
    public int target_fish_id;
    public string start_time;
    public string end_time;
    public string reward_currency;
    public int prize_1st;
    public int prize_2nd;
    public int prize_3rd;
    public int prize_4th;
    public int prize_5th;
    public int prize_6th;
    public int prize_7th;
    public int prize_8th;
    public int prize_9th;
    public int prize_10th;
    public string created_at;
    public CompetitionParticipantResponse[] leaderboard;
}

[Serializable]
public class UpdateCompetitionScoreRequest
{
    public string competition_id;
    public string user_id;
    public string user_name;
    public int score;
}

[Serializable]
public class GenerateCompetitionsRequest
{
    public int count;
}

#nullable disable
