namespace MapEditor
{
    public static class Extensions
    {
        public static string Limit(this string s, int limit)
        {
            if (s == null) return null;
            if (s.Length > limit) return s.Substring(0, limit);
            return s;
        }
    }
}