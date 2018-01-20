﻿using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
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
        /// Constructor
        /// </summary>
        /// <param name="runningManager">Running manager</param>
        /// <param name="context">SharePoint context</param>
        /// <param name="tenant">Tenant</param>
        public TenantRunner(RunningManager runningManager, ClientContext context, Tenant tenant) : base(runningManager, context, tenant, RunningLevel.Tenant) { }

        /// <summary>
        /// Action for this SharePoint tenant
        /// </summary>
        public override void Process()
        {
            RunningManager.Logger.Debug($"TenantRunner Process() - {ActiveReceivers.Count} active receivers");
            Context.Load(Element,
                t => t.RootSiteUrl);
            Context.ExecuteQuery();
            RunningManager.Logger.Debug($"Tenant | URL: {Element.RootSiteUrl}");

            // OnTenantRunningStart
            RunningManager.Logger.Debug("TenantRunner OnTenantRunningStart()");
            ActiveReceivers.ForEach(r => r.OnTenantRunningStart(Element));

            // If at least one receiver run term stores or deeper
            if (Manager.Receivers.Any(r => r.IsReceiverCalledOrDeeper(RunningLevel.TermStore)))
            {
                // Open taxonomy session
                TaxonomySession taxSession = TaxonomySession.GetTaxonomySession(Context);

                // Crawl term stores 
                Context.Load(taxSession,
                    s => s.TermStores);
                Context.ExecuteQuery();

                List<TermStoreRunner> termStoreRunners = new List<TermStoreRunner>();
                foreach (TermStore store in taxSession.TermStores)
                {
                    termStoreRunners.Add(new TermStoreRunner(Manager, Context, store));
                }

                termStoreRunners.ForEach(r => r.Process());
            }

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

                siteCollectionRunners.ForEach(r => r.Process());
            }

            // OnTenantRunningEnd
            RunningManager.Logger.Debug("TenantRunner OnTenantRunningEnd()");
            ActiveReceivers.ForEach(r => r.OnTenantRunningEnd(Element));
        }
    }
}
