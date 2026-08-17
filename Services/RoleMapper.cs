namespace FixMyTownApi.Services
{
    /// <summary>
    /// GRP-03-39DB stores Role as "Citizen"/"Admin"/"Worker" (capitalized),
    /// but every [Authorize(Roles = "citizen")] attribute, JWT claim, and
    /// the whole React frontend was built around lowercase role strings.
    /// Rather than touch dozens of files on both sides, this one small
    /// helper translates at the database boundary only - the rest of the
    /// app never needs to know the database capitalizes them differently.
    /// </summary>
    public static class RoleMapper
    {
        /// <summary>"citizen" -> "Citizen" (for WHERE clauses and inserts).</summary>
        public static string ToDb(string appRole) =>
            appRole.Length == 0 ? appRole : char.ToUpper(appRole[0]) + appRole[1..].ToLower();

        /// <summary>"Citizen" -> "citizen" (for JWT claims and API responses).</summary>
        public static string ToApp(string dbRole) => dbRole.ToLower();
    }
}
