namespace Bulkivore.Api.Endpoints.Common;

public static class StringExtensions
{
    extension(string str)
    {
        public ReadOnlySpan<char> RemoveSuffix(string target)
        {
            if (string.IsNullOrEmpty(str))
                return [];
            if (string.IsNullOrEmpty(target))
                return str.AsSpan();

            return str.EndsWith(target)
                ? str.AsSpan(0, str.Length - target.Length)
                : str.AsSpan();
        }
    }
}
