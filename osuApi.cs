namespace osu_Api;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
public class Mod {
    public required string Acronym;
}
public class Mods
{
    public const int NM = 0;
    public const int NF = 1 << 0;
    public const int EZ = 1 << 1;
    public const int TD = 1 << 2;
    public const int HD = 1 << 3;
    public const int HR = 1 << 4;
    public const int SD = 1 << 5;
    public const int DT = 1 << 6;
    public const int RX = 1 << 7;
    public const int HT = 1 << 8;
    public const int _NC = 1 << 9;
    public const int NC = _NC + DT;
    public const int FL = 1 << 10;
    public const int AT = 1 << 11;
    public const int SO = 1 << 12;
    public const int AP = 1 << 13;
    public const int _PF = 1 << 14;
    public const int PF = _PF + SD;

    public int this[string acronym]
    {
        get
        {
            return acronym switch
            {
                "NM" => NM,
                "NF" => NF,
                "EZ" => EZ,
                "TD" => TD,
                "HD" => HD,
                "HR" => HR,
                "SD" => SD,
                "DT" => DT,
                "RX" => RX,
                "HT" => HT,
                "NC" => NC,
                "FL" => FL,
                "AT" => AT,
                "SO" => SO,
                "AP" => AP,
                "PF" => PF,
                _ => throw new ArgumentException("Invalid mod acronym", acronym),
            };
        }
    }
}

public class User
{
    public int GlobalRank;
    public int CountryRank;
    public required string CountryCode;
    public int Id;
    public int[]? RankHistory;
    public string? username;

}
public class Judgements
{
    public int GreatCount;
    public int OkCount;
    public int MehCount;
    public int MissCount;
}
public class Score
{
    public double PP;
    public double Accuracy;
    public int MaxCombo;
    public required string Rank;
    public Mod[]? Mods;
    public required string ScoreUrl;
    public required Judgements Judgements;
    public string? EmojiMods;
    public DateTime PlayTime;
    public long Id;
    
}
public class Beatmap
{

    public required string Difficulty;
    public required string Url;
    public required string FormattedLength;
    public long Id;
    public required BeatmapAttributes Attributes;
}
public class BeatmapAttributes
{
    public double ApproachRate;
    public double CircleSize;
    public double OverallDifficulty;
    public double HPDrain;
    public double BPM;
    public double StarRating;
    public int MaxCombo;
    public int Length;
}
public class ApiClient(string clientId, string clientSecret)
{
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string _clientId = clientId;
        private readonly string _clientSecret = clientSecret;
        private string? _accessToken;
    public async Task AuthenticateAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://osu.ppy.sh/oauth/token")
            {
                Content = new StringContent($"client_id={_clientId}&client_secret={_clientSecret}&grant_type=client_credentials&scope=public")
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(content) ?? throw new InvalidOperationException("Failed to retrieve access token.");
            _accessToken = tokenResponse.AccessToken;
        }

        public async Task<User> GetUserAsync(string userId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://osu.ppy.sh/api/v2/users/{userId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            JsonElement userJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            int globalRank = userJson.GetProperty("statistics").GetProperty("global_rank").GetInt32();
            int countryRank = userJson.GetProperty("statistics").GetProperty("country_rank").GetInt32();
            string countryCode = userJson.GetProperty("country_code").GetString() ?? string.Empty;
            int id = userJson.GetProperty("id").GetInt32();
            string? username = userJson.GetProperty("username").GetString();
            
            List<int> ranks = userJson.GetProperty("rank_history").EnumerateArray().Select(x => x.GetInt32()).ToList();
            if (username == null) return new User {
                GlobalRank = globalRank,
                CountryRank = countryRank,
                CountryCode = countryCode,
                Id = id,
                RankHistory = [.. ranks],
            };
            return new User
            {
                GlobalRank = globalRank,
                CountryRank = countryRank,
                CountryCode = countryCode,
                Id = id,
                RankHistory = [.. ranks],
                username = username

            };
        
        }

        private class TokenResponse
        {
            [JsonProperty("access_token")]
            public required string AccessToken { get; set; }
        }
        public async Task<bool> IsLazerScore(string scoreID) {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://osu.ppy.sh/api/v2/scores/{scoreID}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("x-api-version", "20220705");
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        private static double GetPP(JsonElement scoreJson) {
            try {
                // Check if pp property exists and is not null
                if (scoreJson.TryGetProperty("pp", out JsonElement ppElement) && ppElement.ValueKind != JsonValueKind.Null) {
                    return ppElement.GetDouble();
                }
                return 0; // Return 0 if pp is null or property doesn't exist
            } catch (Exception ex) {
                Console.WriteLine($"Error getting PP value: {ex.Message}");
                return 0;
            }
        }
        private static DateTime GetPlayTime(JsonElement scoreJson) {
            DateTime playTime = DateTime.UnixEpoch;
            try {
                playTime = scoreJson.GetProperty("created_at").GetDateTime();
                
            } catch (KeyNotFoundException) {
                try {
                    playTime = scoreJson.GetProperty("ended_at").GetDateTime();
                } catch (KeyNotFoundException) {
                    Console.WriteLine("No play time");
                }
            }
            return playTime;
        }
        private static Mod[] GetMods(JsonElement scoreJson) {
            JsonElement modsElement = scoreJson.GetProperty("mods");
            Mod?[] scoreMods = new Mod?[modsElement.GetArrayLength()];
            try  {
                scoreMods = JsonConvert.DeserializeObject<Mod?[]>(modsElement.GetRawText()) ?? [];
                scoreMods = scoreMods.ToArray()
                    .Select(mod => mod != null ? new Mod { Acronym = mod.Acronym } : null)
                    .ToArray();
                
                
            } catch (JsonSerializationException) {
                
                for (int i = 0; i < modsElement.GetArrayLength(); i++) {
                    string? modAcronym = modsElement[i].GetString();
                    if (modAcronym != null)
                    {
                        Mod mod = new() { Acronym = modAcronym };
                        scoreMods[i] = mod;
                    }
                }
            }
            return scoreMods.Select(mod => mod!).ToArray();
        }
        private static Judgements GetJudgements(JsonElement scoreJson) {
            int greatCount = 0;
            try {
                greatCount = scoreJson.GetProperty("statistics").GetProperty("great").GetInt32();
            } catch (KeyNotFoundException) {
                try {
                    greatCount = scoreJson.GetProperty("statistics").GetProperty("count_300").GetInt32();
                } catch (KeyNotFoundException) {
                }
            }
            int okCount = 0;
            try {
                okCount = scoreJson.GetProperty("statistics").GetProperty("ok").GetInt32();
            } catch (KeyNotFoundException) {
                try {
                    okCount = scoreJson.GetProperty("statistics").GetProperty("count_100").GetInt32();
                } catch (KeyNotFoundException) {
                }
            }
            int mehCount = 0;
            try {
                mehCount = scoreJson.GetProperty("statistics").GetProperty("meh").GetInt32();
            } catch (KeyNotFoundException) {
                try {
                    mehCount = scoreJson.GetProperty("statistics").GetProperty("count_50").GetInt32();
                } catch (KeyNotFoundException) {
            
                }
            }
            int missCount = 0;
            try {
                missCount = scoreJson.GetProperty("statistics").GetProperty("miss").GetInt32();
            } catch (KeyNotFoundException) {
                try {
                    missCount = scoreJson.GetProperty("statistics").GetProperty("count_miss").GetInt32();
                } catch (KeyNotFoundException) {
                }
            }
            return new Judgements
            {
                GreatCount = greatCount,
                OkCount = okCount,
                MehCount = mehCount,
                MissCount = missCount
            };
        }
        public async Task<Score> GetScoreAsync(string scoreId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://osu.ppy.sh/api/v2/scores/{scoreId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("x-api-version", "20220705");
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                // Fallback to legacy score getting
                request = new HttpRequestMessage(HttpMethod.Get, $"https://osu.ppy.sh/api/v2/scores/osu/{scoreId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                request.Headers.Add("Accept", "application/json");
                response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }

            JsonElement scoreJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            double pp = GetPP(scoreJson);
            double acc = scoreJson.GetProperty("accuracy").GetDouble();
            int maxCombo = scoreJson.GetProperty("max_combo").GetInt32();
            string rank = scoreJson.GetProperty("rank").GetString() ?? string.Empty;
            Mod[] mods = GetMods(scoreJson);
            long Id = scoreJson.GetProperty("id").GetInt64();
            string scoreUrl;
            if (await IsLazerScore(Id.ToString())) {
                scoreUrl = $"https://osu.ppy.sh/scores/{Id}";
            }
            else {
                scoreUrl = $"https://osu.ppy.sh/scores/osu/{Id}";
            }

            DateTime playtime = GetPlayTime(scoreJson);
            Judgements judgements = GetJudgements(scoreJson);
            return new Score
            {
                PP = pp,
                Accuracy = acc,
                MaxCombo = maxCombo,
                Rank = rank,
                Mods = mods,
                ScoreUrl = scoreUrl,
                Judgements = judgements,
                PlayTime = playtime,
                Id = Id,
            };
        }
        
        private static BeatmapAttributes Process_mods(Mod[] mods, BeatmapAttributes attributes) {
            foreach (Mod mod in mods) {
                switch (mod.Acronym) {
                    case "HR":
                        attributes.HPDrain = Math.Min(10,attributes.HPDrain *= 1.4);
                        attributes.CircleSize *= 1.3;
                        break;
                    case "HD":
                        break;
                    case "DT":
                        attributes.BPM *= 1.5;
                        attributes.Length = (int)(attributes.Length / 1.5);
                        break;
                    case "FL":
                        break;
                    case "EZ":
                        attributes.CircleSize *= 0.5;
                        attributes.HPDrain *= 0.5;
                        break;
                    case "NF":
                        break;
                    case "HT":
                        attributes.Length = (int)(attributes.Length * 1.5);
                        attributes.BPM /= 1.5;
                        break;
                    case "SO":
                        break;
                    case "NC":
                        attributes.BPM *= 1.5;
                        attributes.Length = (int)(attributes.Length / 1.5);
                        break;
                    case "SD":
                        break;
                    case "PF":
                        break;
                }
            }
            return attributes;
        }
        public async Task<BeatmapAttributes> GetBeatmapAttributesAsync(string beatmapId,Mod[] mods,JsonElement beatmapJson)
        {   
            int modValue = 0;
            foreach (Mod mod in mods)
            {
                modValue |= new Mods()[mod.Acronym];
            }
            /* 
                curl --request POST \
                "https://osu.ppy.sh/api/v2/beatmaps/2/attributes" \
                --header "Content-Type: application/json" \
                --header "Accept: application/json" \
                --data "{
                \"mods\": 1,
                \"ruleset\": \"osu\"
                }"
            */
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://osu.ppy.sh/api/v2/beatmaps/{beatmapId}/attributes");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            request.Headers.Add("Accept", "application/json");

            // {"mods": 72, "ruleset": "osu"}
            request.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new { mods = modValue, ruleset = "osu" }),
                                                System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            JsonElement beatmapAttributesJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            double cs = beatmapJson.GetProperty("cs").GetDouble();
            double ar = beatmapAttributesJson.GetProperty("approach_rate").GetDouble();
            double od = beatmapAttributesJson.GetProperty("overall_difficulty").GetDouble();
            double hp = beatmapJson.GetProperty("drain").GetDouble();
            double bpm = beatmapJson.GetProperty("bpm").GetDouble();
            double sr = beatmapAttributesJson.GetProperty("star_rating").GetDouble();
            int length = beatmapJson.GetProperty("total_length").GetInt32();
            int max_combo = beatmapAttributesJson.GetProperty("max_combo").GetInt32();

            BeatmapAttributes beatmapAttributes = new()
            {
                ApproachRate = ar,
                CircleSize = cs,
                OverallDifficulty = od,
                HPDrain = hp,
                BPM = bpm,
                StarRating = sr,
                MaxCombo = max_combo,
                Length = length
            };
            return Process_mods(mods,beatmapAttributes);

        }   
        
        public async Task<Beatmap> GetBeatmapAsync(string beatmapId,Mod[] mods)
        {
            Console.WriteLine(beatmapId);
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://osu.ppy.sh/api/v2/beatmaps/{beatmapId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            request.Headers.Add("Accept", "application/json");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            JsonElement beatmapJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            string difficulty = beatmapJson.GetProperty("version").GetString() ?? string.Empty;
            string url = $"https://osu.ppy.sh/beatmaps/{beatmapId}";
            int length = beatmapJson.GetProperty("total_length").GetInt32();
            TimeSpan time = TimeSpan.FromSeconds(length);
            string formattedLength = time.ToString(@"mm\:ss");
            BeatmapAttributes attributes = await GetBeatmapAttributesAsync(beatmapId,mods,beatmapJson);
            return new Beatmap
            {
                Difficulty = difficulty,
                Url = url,
                FormattedLength = formattedLength,
                Attributes = attributes,
                Id = long.Parse(beatmapId)
            };
        }
        public async Task<string> GetScores(string userId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://osu.ppy.sh/api/v2/users/{userId}/scores/recent");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        public async Task<string> GetScores(string userId, string cursorString)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://osu.ppy.sh/api/v2/users/{userId}/scores/recent?cursor_string={cursorString}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
}
