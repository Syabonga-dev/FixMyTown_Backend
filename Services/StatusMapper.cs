namespace FixMyTownApi.Services
{
    /// <summary>
    /// GRP-03-39DB's CK_Reports_Status constraint only allows:
    /// Reported, UnderReview, Assigned, InProgress, Resolved, Closed, Rejected
    /// (no spaces). The frontend (StatusBadge.jsx and everywhere else) was
    /// built around "Reported"/"Assigned"/"In Progress"/"Resolved" (note
    /// the space in "In Progress"). This helper translates at the database
    /// boundary so the frontend never has to change.
    ///
    /// Our app only ever uses 4 of the 7 possible database values -
    /// UnderReview/Closed/Rejected exist in the schema but nothing in the
    /// current frontend creates or displays them.
    /// </summary>
    public static class StatusMapper
    {
        /// <summary>App-facing string -> what's stored in the database.</summary>
        public static string ToDb(string appStatus) => appStatus switch
        {
            "In Progress" => "InProgress",
            _ => appStatus // Reported, Assigned, Resolved need no change
        };

        /// <summary>Database value -> what the frontend expects to see.</summary>
        public static string ToApp(string dbStatus) => dbStatus switch
        {
            "InProgress" => "In Progress",
            "UnderReview" => "Reported",   // frontend has no separate "under review" state
            "Closed" => "Resolved",        // frontend has no separate "closed" state
            "Rejected" => "Reported",      // frontend has no separate "rejected" state
            _ => dbStatus                  // Reported, Assigned, Resolved need no change
        };
    }
}
