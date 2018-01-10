﻿using Microsoft.SharePoint.Client;
using SharePointRunner.SDK;
using System.Collections.Generic;
using System.Linq;

namespace SharePointRunner
{
    internal class SiteRunner : Runner<Web>
    {
        /// <summary>
        /// Running level
        /// </summary>
        public override RunningLevel RunningLevel => RunningLevel.Site;

        /// <summary>
        /// List of active receivers for this runner
        /// </summary>
        protected override List<Receiver> ActiveReceivers
        {
            get
            {
                if (IsSubSite)
                {
                    return base.ActiveReceivers.Where(r => r.IncludeSubSites).ToList();
                }
                else
                {
                    return base.ActiveReceivers;
                }
            }
        }

        /// <summary>
        /// True if the site is a sub site, False if not
        /// </summary>
        public virtual bool IsSubSite { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="runningManager">Running manager</param>
        /// <param name="context">SharePoint context</param>
        /// <param name="web">Site</param>
        /// <param name="isSubSite">True if the site is a sub site, False if not</param>
        public SiteRunner(RunningManager runningManager, ClientContext context, Web web, bool isSubSite = false) : base(runningManager, context, web)
        {
            IsSubSite = isSubSite;
        }

        /// <summary>
        /// Action for this SharePoint site
        /// </summary>
        public override void Process()
        {
            Context.Load(Element);
            Context.ExecuteQuery();

            // OnSiteRunningStart
            ActiveReceivers.ForEach(r => r.OnSiteRunningStart(Element));

            // If at least one receiver run lists or deeper
            if (Manager.Receivers.Any(r => r.IsReceiverCalledOrDeeper(RunningLevel.List)))
            {
                // Crawl Lists
                Context.Load(Element.Lists);
                Context.ExecuteQuery();

                List<ListRunner> listRunners = new List<ListRunner>();
                foreach (List list in Element.Lists)
                {
                    listRunners.Add(new ListRunner(Manager, Context, list));
                }

                listRunners.ForEach(a => a.Process());
            }

            // OnSiteRunningEnd
            ActiveReceivers.ForEach(r => r.OnSiteRunningEnd(Element));

            // If at least one receiver run subsites
            if (Manager.Receivers.Any(r => r.IncludeSubSites))
            {
                // Crawl Subsites
                // -----------------------------------------------------------
                Context.Load(Element, e => e.Features.Include(
                    f => f.DefinitionId,
                    f => f.DisplayName));

                Context.ExecuteQuery();
                // -----------------------------------------------------------

                Context.Load(Element.Webs);
                Context.ExecuteQuery();

                List<SiteRunner> siteRunners = new List<SiteRunner>();
                foreach (Web subWeb in Element.Webs)
                {
                    siteRunners.Add(new SiteRunner(Manager, Context, subWeb, true));
                }

                siteRunners.ForEach(a => a.Process());

                // OnSiteRunningEndAfterSubSites
                ActiveReceivers.ForEach(r => r.OnSiteRunningEndAfterSubSites(Element));
            }
        }
    }
}
