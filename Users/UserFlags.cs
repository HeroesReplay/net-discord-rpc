namespace NetDiscordRpc.Users
{
    public enum UserFlags
    {
        None = 0,
        Employee = 1 << 0,
        Partner = 1 << 1,
        HypeSquad = 1 << 2,
        BugHunter = 1 << 3,
        HouseBravery = 1 << 6,
        HouseBrilliance = 1 << 7,
        HouseBalance = 1 << 8,
        EarlySupporter = 1 << 9,
        TeamUser = 1 << 10
    }
}