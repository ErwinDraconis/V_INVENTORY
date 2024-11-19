namespace ValeoItacCheck
{
    public static class StringExtensions
    {
        public static string Between(this string str, string firstString, string lastString)
        {
            if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(firstString) || string.IsNullOrEmpty(lastString))
                return string.Empty;

            int pos1 = str.IndexOf(firstString);
            if (pos1 == -1) return string.Empty; 

            pos1 += firstString.Length;
            int pos2 = str.IndexOf(lastString, pos1);
            if (pos2 == -1) return string.Empty; 

            return str.Substring(pos1, pos2 - pos1);
        }
    }

}
