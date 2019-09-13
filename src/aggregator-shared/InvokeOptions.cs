using System;
using System.Collections.Specialized;
using System.Globalization;

using Microsoft.WindowsAzure.Storage.Core;

namespace aggregator
{
    public static class InvokeOptions
    {
        public static Uri AddToUrl(this Uri ruleUrl, bool dryRun = false, SaveMode saveMode = SaveMode.Default)
        {
            var queryBuilder = new UriQueryBuilder();
            queryBuilder.Add("dryRun", dryRun.ToString(CultureInfo.InvariantCulture));
            queryBuilder.Add("saveMode", saveMode.ToString());

            return queryBuilder.AddToUri(ruleUrl);
        }

        public static AggregatorConfiguration UpdateFromUrl(this AggregatorConfiguration configuration, Uri requestUri)
        {
            var parameters = System.Web.HttpUtility.ParseQueryString(requestUri.Query);

            configuration.DryRun = IsDryRunEnabled(parameters);

            if (Enum.TryParse(parameters["saveMode"], out SaveMode saveMode))
            {
                configuration.SaveMode = saveMode;
            }

            return configuration;
        }

        private static bool IsDryRunEnabled(NameValueCollection parameters)
        {
            bool dryRun = bool.TryParse(parameters["dryRun"], out dryRun) && dryRun;
            return dryRun;
        }
    }
}
