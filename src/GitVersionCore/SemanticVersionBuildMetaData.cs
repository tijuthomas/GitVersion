namespace GitVersion
{
    using System;
    using System.Text.RegularExpressions;

    public class SemanticVersionBuildMetaData : IFormattable, IEquatable<SemanticVersionBuildMetaData>
    {
        static Regex ParseRegex = new Regex(
            @"(?<BuildNumber>\d+)?(\.?Branch(Name)?\.(?<BranchName>[^\.]+))?(\.?Sha?\.(?<Sha>[^\.]+))?(?<Other>.*)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        static LambdaEqualityHelper<SemanticVersionBuildMetaData> equalityHelper =
           new LambdaEqualityHelper<SemanticVersionBuildMetaData>(x => x.CommitsSinceTag, x => x.Branch, x => x.Sha);

        public int? CommitsSinceTag;
        public string Branch;
        public string Sha;
        public string OtherMetaData;
        public DateTimeOffset CommitDate;
        public int CommitsSinceVersionSource;

        public SemanticVersionBuildMetaData()
        {
        }

        public SemanticVersionBuildMetaData(int? commitsSinceTag, string branch, string commitSha, DateTimeOffset commitDate, string otherMetadata = null)
        {
            Sha = commitSha;
            CommitsSinceTag = commitsSinceTag;
            Branch = branch;
            CommitDate = commitDate;
            OtherMetaData = otherMetadata;
            CommitsSinceVersionSource = commitsSinceTag ?? 0;
        }

        public SemanticVersionBuildMetaData(SemanticVersionBuildMetaData buildMetaData)
        {
            Sha = buildMetaData.Sha;
            CommitsSinceTag = buildMetaData.CommitsSinceTag;
            Branch = buildMetaData.Branch;
            CommitDate = buildMetaData.CommitDate;
            OtherMetaData = buildMetaData.OtherMetaData;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SemanticVersionBuildMetaData);
        }

        public bool Equals(SemanticVersionBuildMetaData other)
        {
            return equalityHelper.Equals(this, other);
        }

        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode(this);
        }

        public override string ToString()
        {
            return ToString(null);
        }

        /// <summary>
        /// <para>b - Formats just the build number</para>
        /// <para>s - Formats the build number and the Git Sha</para>
        /// <para>f - Formats the full build metadata</para>
        /// <para>p - Formats the padded build number. Can specify an integer for padding, default is 4. (i.e., p5)</para>
        /// </summary>
        public string ToString(string format, IFormatProvider formatProvider = null)
        {
            if (formatProvider != null)
            {
                var formatter = formatProvider.GetFormat(GetType()) as ICustomFormatter;

                if (formatter != null)
                    return formatter.Format(format, this, formatProvider);
            }

            if (string.IsNullOrEmpty(format))
                format = "b";


            format = format.ToLower();
            if (format.StartsWith("p", StringComparison.Ordinal))
            {
                // Handle format
                var padding = 4;
                if (format.Length > 1)
                {
                    // try to parse
                    int p;
                    if (int.TryParse(format.Substring(1), out p))
                    {
                        padding = p;
                    }
                }

                return CommitsSinceTag != null ? CommitsSinceTag.Value.ToString("D" + padding) : string.Empty; 
            }
            
            switch (format.ToLower())
            {
                case "b":
                    return CommitsSinceTag.ToString();
                case "s":
                    return string.Format("{0}{1}", CommitsSinceTag, string.IsNullOrEmpty(Sha) ? null : ".Sha." + Sha).TrimStart('.');
                case "f":
                    return string.Format(
                        "{0}{1}{2}{3}",
                        CommitsSinceTag, 
                        string.IsNullOrEmpty(Branch) ? null : ".Branch." + Branch,
                        string.IsNullOrEmpty(Sha) ? null : ".Sha." + Sha,
                        string.IsNullOrEmpty(OtherMetaData) ? null : "." + OtherMetaData)
                        .TrimStart('.');
                default:
                    throw new ArgumentException("Unrecognised format", "format");
            }
        }

        public static bool operator ==(SemanticVersionBuildMetaData left, SemanticVersionBuildMetaData right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SemanticVersionBuildMetaData left, SemanticVersionBuildMetaData right)
        {
            return !Equals(left, right);
        }

        public static implicit operator string(SemanticVersionBuildMetaData preReleaseTag)
        {
            return preReleaseTag.ToString();
        }

        public static implicit operator SemanticVersionBuildMetaData(string preReleaseTag)
        {
            return Parse(preReleaseTag);
        }

        public static SemanticVersionBuildMetaData Parse(string buildMetaData)
        {
            var semanticVersionBuildMetaData = new SemanticVersionBuildMetaData();
            if (string.IsNullOrEmpty(buildMetaData))
                return semanticVersionBuildMetaData;

            var parsed = ParseRegex.Match(buildMetaData);

            if (parsed.Groups["BuildNumber"].Success)
            {
                semanticVersionBuildMetaData.CommitsSinceTag = int.Parse(parsed.Groups["BuildNumber"].Value);
                semanticVersionBuildMetaData.CommitsSinceVersionSource = semanticVersionBuildMetaData.CommitsSinceTag ?? 0;
            }

            if (parsed.Groups["BranchName"].Success)
                semanticVersionBuildMetaData.Branch = parsed.Groups["BranchName"].Value;

            if (parsed.Groups["Sha"].Success)
                semanticVersionBuildMetaData.Sha = parsed.Groups["Sha"].Value;

            if (parsed.Groups["Other"].Success && !string.IsNullOrEmpty(parsed.Groups["Other"].Value))
                semanticVersionBuildMetaData.OtherMetaData = parsed.Groups["Other"].Value.TrimStart('.');

            return semanticVersionBuildMetaData;
        }
    }
}
