using System.Collections;

namespace ViciOne.DeviceTree.Contracts.Comparer;

//Todo: extract together with the version in Clustereditor

/// <summary>
/// Represents an alphanumeric string comparison operation.
/// </summary>
public sealed class AlphaNumericComparer : IComparer
{
    /// <summary>
    /// Returns the instance of the <see cref="AlphaNumericComparer"/>.
    /// </summary>
    public static AlphaNumericComparer Default { get; } = new AlphaNumericComparer();

    private AlphaNumericComparer() { }

    /// <summary>
    /// Compares two objects or strings and returns an indication of their relative sort order based on an
    /// alphanumeric comparison.
    /// </summary>
    /// <param name="x">An object to compare with <paramref name="y"/>.</param>
    /// <param name="y">An object to compare with <paramref name="x"/>.</param>
    /// <returns>A signed integer indicating the relative values of <paramref name="x"/> and <paramref name="y"/>.</returns>
    public int Compare(object? x, object? y)
    {
        if (x is null || y is null)
            return 0;

        string? s1 = x.ToString();
        string? s2 = y.ToString();

        if (x is DictionaryEntry entry1)
            s1 = entry1.Key.ToString();

        if (y is DictionaryEntry entry2)
            s2 = entry2.Key.ToString();

        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0;

        int length1 = s1.Length;
        int length2 = s2.Length;

        int marker1 = 0;
        int marker2 = 0;

        char char1;
        char char2;

        int index1;
        int index2;

        char[] space1;
        char[] space2;

        // Walk both strings using 2 markers
        while (marker1 < length1 && marker2 < length2)
        {
            char1 = s1[marker1];
            char2 = s2[marker2];

            // Set up buffers and variables
            index1 = 0;
            index2 = 0;

            space1 = new char[length1];
            space2 = new char[length2];

            // Walk the strings and pick out all digit runs
            do
            {
                space1[index1++] = char1;
                marker1++;

                if (marker1 < length1)
                    char1 = s1[marker1];
                else
                    break;
            } while (char.IsDigit(char1) == char.IsDigit(space1[0]));

            do
            {
                space2[index2++] = char2;
                marker2++;

                if (marker2 < length2)
                    char2 = s2[marker2];
                else
                    break;
            } while (char.IsDigit(char2) == char.IsDigit(space2[0]));

            // Build the strings - if they start with digits, compare numerically, otherwise alphanumerically
            int result;
            if (char.IsDigit(space1[0]) && char.IsDigit(space2[0]))
            {
                _ = long.TryParse(new string(space1), out long long1);
                _ = long.TryParse(new string(space2), out long long2);

                result = long1.CompareTo(long2);
            }
            else
                result = string.Compare(new string(space1), new string(space2), StringComparison.OrdinalIgnoreCase);

            if (result != 0)
                return result;
        }

        return length1 - length2;
    }
}

/// <summary>
/// Represents an alphanumeric string comparison operation.
/// </summary>
public sealed class AlphaNumericComparer<T> : IComparer<T>
{
    private static readonly AlphaNumericComparer s_comparer = AlphaNumericComparer.Default;

    /// <summary>
    /// Returns the default alphanumeric sort-order comparer for the type given by the generic argument.
    /// </summary>
    public static AlphaNumericComparer<T> Default { get; } = new AlphaNumericComparer<T>();

    private AlphaNumericComparer() { }

    /// <summary>
    /// Compares two objects or strings and returns an indication of their relative sort order based on an
    /// alphanumeric comparison.
    /// </summary>
    /// <param name="x">An object to compare with <paramref name="y"/>.</param>
    /// <param name="y">An object to compare with <paramref name="x"/>.</param>
    /// <returns>A signed integer indicating the relative values of <paramref name="x"/> and <paramref name="y"/>.</returns>
    public int Compare(T? x, T? y) => s_comparer.Compare(x, y);
}
