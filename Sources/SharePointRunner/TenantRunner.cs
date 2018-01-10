﻿using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using SharePointRunner.SDK;
using System.Collections.Generic;
using System.Linq;

namespace SharePointRunner
{
    /// <summary>
    /// Runner of SharePoint Tenant
    /// </summary>
    internal class TenantRunner : Runner<Tenant>
    {
        /// <summary>
        /// Running level
        /// </summary>
        public override RunningLevel RunningLevel => RunningLevel.Tenant;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="runningManager">Running manager</param>
        /// <param name="context">SharePoint context</param>
        /// <param name="tenant">Tenant</param>
        public TenantRunner(RunningManager runningManager, ClientContext context, Tenant tenant) : base(runningManager, context, tenant) { }

        /// <summary>
        /// Action for this SharePoint tenant
        /// </summary>
        public override void Process()
        {
            Context.Load(Element);
            Context.ExecuteQuery();

            // OnTenantRunningStart
            ActiveReceivers.ForEach(r => r.OnTenantRunningStart(Element));

            // If at least one receiver run site collections or deeper
            if (Manager.Receivers.Any(r => r.IsReceiverCalledOrDeeper(RunningLevel.SiteCollection)))
            {
                // Get site collections URLs
                SPOSitePropertiesEnumerable properties = Element.GetSiteProperties(0, true);
                Context.Load(properties);
                Context.ExecuteQuery();

                // Crawl among site collections
                List<string> siteCollectionUrls = properties.Select(p => p.Url).ToList();

                List<SiteCollectionRunner> siteCollectionRunners = new List<SiteCollectionRunner>();
                foreach (string siteCollectionUrl in siteCollectionUrls)
                {

                    ClientContext ctx = new ClientContext(siteCollectionUrl)
                    {
                        Credentials = Context.Credentials
                    };
                    siteCollectionRunners.Add(new SiteCollectionRunner(Manager, ctx, ctx.Site));
                }

                siteCollectionRunners.ForEach(a => a.Process());
            }

            // OnTenantRunningEnd
            ActiveReceivers.ForEach(r => r.OnTenantRunningEnd(Element));
        }
    }
}