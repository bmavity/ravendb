//-----------------------------------------------------------------------
// <copyright file="AuthorizationDeleteTrigger.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.IO;
using System.Web;
using Newtonsoft.Json.Linq;
using Raven.Database;
using Raven.Database.Plugins;
using Raven.Http;

namespace Raven.Bundles.Authorization.Triggers
{
	public class AuthorizationDeleteTrigger : AbstractDeleteTrigger
	{
		public AuthorizationDecisions AuthorizationDecisions { get; set; }

		public override void Initialize()
		{
			AuthorizationDecisions = new AuthorizationDecisions(Database);
		}

		public override VetoResult AllowDelete(string key, TransactionInformation transactionInformation)
		{
			if (AuthorizationContext.IsInAuthorizationContext)
				return VetoResult.Allowed;

			using(AuthorizationContext.Enter())
			{
				var user = CurrentOperationContext.Headers.Value[Constants.RavenAuthorizationUser];
                var operation = CurrentOperationContext.Headers.Value[Constants.RavenAuthorizationOperation];
				if (string.IsNullOrEmpty(operation) || string.IsNullOrEmpty(user))
					return VetoResult.Allowed;

				var previousDocument = Database.Get(key, transactionInformation);
				if (previousDocument == null)
					return VetoResult.Allowed;

				var sw = new StringWriter();
				var isAllowed = AuthorizationDecisions.IsAllowed(user, operation, key, previousDocument.Metadata, sw.WriteLine);
				return isAllowed ?
					VetoResult.Allowed :
					VetoResult.Deny(sw.GetStringBuilder().ToString());
			}
		}
	}
}
