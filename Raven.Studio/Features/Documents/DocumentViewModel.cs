﻿namespace Raven.Studio.Features.Documents
{
	using System;
	using Collections;
	using Framework;
	using Newtonsoft.Json.Linq;
	using Raven.Database;

	/// <summary>
	/// This view model is for displaying documents in bulk. There is no change notification and no behaviours related to editing
	/// </summary>
	public class DocumentViewModel : ISupportDocumentTemplate
	{
		const int SummaryLength = 150;
		readonly JsonDocument inner;

		public DocumentViewModel(JsonDocument inner)
		{
			this.inner = inner;
			Id = inner.Metadata.IfPresent<string>("@id");
			LastModified = inner.Metadata.IfPresent<DateTime>("Last-Modified");
			ClrType = inner.Metadata.IfPresent<string>("Raven-Clr-Type");
			CollectionType = DetermineCollectionType();
		}

		public string Id { get; private set; }
		public string ClrType { get; private set; }
		public string CollectionType { get; private set; }
		public DateTime LastModified { get; private set; }

		public string DisplayId
		{
			get
			{
				if (string.IsNullOrEmpty(Id)) return string.Empty;

				var collectionType = CollectionType + "/";
				var display = Id
					.Replace(collectionType, string.Empty)
					.Replace(collectionType.ToLower(), string.Empty)
					.Replace("Raven/", string.Empty);

				Guid guid;
				if (Guid.TryParse(display, out guid))
				{
					display = display.Substring(0, 8);
				}
				return display;
			}
		}

		public JObject Contents
		{
			get { return JsonDocument.DataAsJson; }
		}

		public string Summary
		{
			get
			{
				var json = JsonDocument.DataAsJson.ToString();
				json = (json.Length > SummaryLength)
				       	? json.Substring(0, SummaryLength) + "..."
				       	: json;

				return json
					.Replace("\r", "")
					.Replace("\n", " ")
					.TrimStart('{', ' ')
					.TrimEnd('}');
			}
		}

		public JObject Metadata
		{
			get { return JsonDocument.Metadata; }
		}

		public JsonDocument JsonDocument
		{
			get { return inner; }
		}

		string ISupportDocumentTemplate.TemplateKey
		{
			get { return CollectionType; }
		}

		string DetermineCollectionType()
		{
			return DetermineCollectionType(Metadata);
		}

		public static string DetermineCollectionType(JObject metadata)
		{
			var id = metadata.IfPresent<string>("@id") ?? string.Empty;

			if (string.IsNullOrEmpty(id)) 
				return BuiltinCollectionName.Projection; // meaning that the document is a projection and not a 'real' document

			var entity = metadata.IfPresent<string>("Raven-Entity-Name");
			return entity ?? 
				(id.StartsWith("Raven/") 
					? BuiltinCollectionName.System 
					: BuiltinCollectionName.Document);
		}
	}
}