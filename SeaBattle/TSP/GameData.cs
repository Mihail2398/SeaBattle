using System.Text.Json;
using System.IO;
using NavalBattle.Logics;

namespace NavalBattle.Network
{
    public class GamePacket
    {
        public int X { get; set; }
        public int Y { get; set; }
        public CellState Result { get; set; }
        public bool IsGameOver { get; set; }
    }

    public class UserProfile
    {
        public string Name { get; set; }
        public int Wins { get; set; }
    }

    public static class ProfileManager
    {
        private const string FilePath = "profile.json";
        public static void SaveProfile(UserProfile profile) =>
            File.WriteAllText(FilePath, JsonSerializer.Serialize(profile));

        public static UserProfile LoadProfile() =>
            File.Exists(FilePath) ? JsonSerializer.Deserialize<UserProfile>(File.ReadAllText(FilePath))
            : new UserProfile { Name = "Игрок", Wins = 0 };
    }
}