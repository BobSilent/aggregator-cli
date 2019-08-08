﻿using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace aggregator.Engine
{
    public class WorkItemWrapper
    {
        private EngineContext _context;
        private WorkItem _item;
        private WorkItemRelationWrapperCollection _relationCollection;

        internal WorkItemWrapper(EngineContext context, WorkItem item)
        {
            _context = context;
            _item = item;
            _relationCollection = new WorkItemRelationWrapperCollection(this, _item.Relations);

            if (item.Id.HasValue)
            {
                Id = new PermanentWorkItemId(item.Id.Value);
                Changes.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Test,
                    Path = "/rev",
                    Value = item.Rev
                });
                //for simplify testing: item.Url can be null
                IsDeleted = item.Url?.EndsWith($"/recyclebin/{item.Id.Value}", StringComparison.OrdinalIgnoreCase) ?? false;

                IsReadOnly = false;
                _context.Tracker.TrackExisting(this);
            }
            else
            {
                Id = new TemporaryWorkItemId(_context.Tracker);
                Changes.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/id",
                    Value = Id.Value
                });

                _context.Tracker.TrackNew(this);
            }
        }


        internal WorkItemWrapper(EngineContext context, WorkItem item, bool isReadOnly)
            // we cannot reuse the code, because tracking is different
            //: this(context, item)
        {
            _context = context;
            _item = item;
            _relationCollection = new WorkItemRelationWrapperCollection(this, _item.Relations);

            Id = new PermanentWorkItemId(item.Id.Value);
            Changes.Add(new JsonPatchOperation()
            {
                Operation = Operation.Test,
                Path = "/rev",
                Value = item.Rev
            });
            IsDeleted = item.Url?.EndsWith($"/recyclebin/{item.Id.Value}", StringComparison.OrdinalIgnoreCase) ?? false;

            IsReadOnly = isReadOnly;
            _context.Tracker.TrackRevision(this);
        }

        public WorkItemWrapper PreviousRevision
        {
            get
            {
                if (Rev > 0)
                {
                    // TODO we shouldn't use the client in this class, move to WorkItemStore.GetRevisionAsync, workitemstore shoud check tracker if already loaded
                    // TODO thinkabout passing workitemstore into workitemwrapper constructor, instead of engineContext, workitemstore is used several times, see also Property Children/Parent
                    var previousRevision = _context.Client.GetRevisionAsync(this.Id.Value, this.Rev - 1, expand: WorkItemExpand.All).Result;
                    return new WorkItemWrapper(_context, previousRevision, true);
                }

                return null;
            }
        }

        public IEnumerable<WorkItemWrapper> Revisions
        {
            get
            {
                // TODO load a few revisions at a time
                //var all = _context.Client.GetRevisionsAsync(this.Id.Value, expand: WorkItemExpand.All).Result;
                var revision = this;
                while ((revision = revision.PreviousRevision) != null)
                {
                    yield return revision;
                }
            }
        }

        public IEnumerable<WorkItemRelationWrapper> RelationLinks
        {
            get
            {
                return _relationCollection;
            }
        }

        public WorkItemRelationWrapperCollection Relations
        {
            get
            {
                return _relationCollection;
            }
        }

        public IEnumerable<WorkItemRelationWrapper> ChildrenLinks
        {
            get
            {
                return _relationCollection
                    .Where(rel => rel.Rel == CoreRelationRefNames.Children);
            }
        }

        public IEnumerable<WorkItemWrapper> Children
        {
            get
            {
                if (ChildrenLinks != null && ChildrenLinks.Count() > 0)
                {
                    var store = new WorkItemStore(_context);
                    return store.GetWorkItems(ChildrenLinks);
                }
                else
                    return new WorkItemWrapper[0];
            }
        }

        public IEnumerable<WorkItemRelationWrapper> RelatedLinks
        {
            get
            {
                return _relationCollection
                    .Where(rel => rel.Rel == CoreRelationRefNames.Related);
            }
        }

        public IEnumerable<WorkItemRelationWrapper> Hyperlinks
        {
            get
            {
                return _relationCollection
                    .Where(rel => rel.Rel == CoreRelationRefNames.Hyperlink);
            }
        }

        public WorkItemRelationWrapper ParentLink
        {
            get
            {
                return _relationCollection
                    .SingleOrDefault(rel => rel.Rel == CoreRelationRefNames.Parent);
            }
        }

        public WorkItemWrapper Parent
        {
            get
            {
                 if (ParentLink != null && ParentLink != default(WorkItemRelationWrapper))
                 {
                     var store = new WorkItemStore(_context);
                     return store.GetWorkItem(ParentLink);
                 }
                 else
                     return null;
            }
        }

        public WorkItemId<int> Id
        {
            get;
            private set;
        }

        public int Rev => _item.Rev.Value;

        public string Url => _item.Url;

        public string WorkItemType
        {
            get { return (string)_item.Fields[CoreFieldRefNames.WorkItemType]; }
            private set { SetFieldValue(CoreFieldRefNames.WorkItemType, value); }
        }

        public string State
        {
            get { return GetFieldValue<string>(CoreFieldRefNames.State); }
            set { SetFieldValue(CoreFieldRefNames.State, value); }
        }

        public int AreaId
        {
            get { return GetFieldValue<int>(CoreFieldRefNames.AreaId); }
            set { SetFieldValue(CoreFieldRefNames.AreaId, value); }
        }

        public string AreaPath
        {
            get { return GetFieldValue<string>(CoreFieldRefNames.AreaPath); }
            set { SetFieldValue(CoreFieldRefNames.AreaPath, value); }
        }

        public IdentityRef AssignedTo
        {
            get { return GetFieldValue<IdentityRef>(CoreFieldRefNames.AssignedTo); }
            set { SetFieldValue(CoreFieldRefNames.AssignedTo, value); }
        }

        public int AttachedFileCount
        {
            get { return GetFieldValue<int>(CoreFieldRefNames.AttachedFileCount); }
            set { SetFieldValue(CoreFieldRefNames.AttachedFileCount, value); }
        }

        public IdentityRef AuthorizedAs
        {
            get { return GetFieldValue<IdentityRef>(CoreFieldRefNames.AuthorizedAs); }
            set { SetFieldValue(CoreFieldRefNames.AuthorizedAs, value); }
        }

        public IdentityRef ChangedBy
        {
            get { return GetFieldValue<IdentityRef>(CoreFieldRefNames.ChangedBy); }
            set { SetFieldValue(CoreFieldRefNames.ChangedBy, value); }
        }

        public DateTime? ChangedDate
        {
            get { return GetFieldValue<DateTime?>(CoreFieldRefNames.ChangedDate); }
            set { SetFieldValue(CoreFieldRefNames.ChangedDate, value); }
        }

        public IdentityRef CreatedBy
        {
            get { return GetFieldValue<IdentityRef>(CoreFieldRefNames.CreatedBy); }
            set { SetFieldValue(CoreFieldRefNames.CreatedBy, value); }
        }

        public DateTime? CreatedDate
        {
            get { return GetFieldValue<DateTime?>(CoreFieldRefNames.CreatedDate); }
            set { SetFieldValue(CoreFieldRefNames.CreatedDate, value); }
        }

        public string Description
        {
            get { return GetFieldValue<string>(CoreFieldRefNames.Description); }
            set { SetFieldValue(CoreFieldRefNames.Description, value); }
        }

        public int ExternalLinkCount
        {
            get { return GetFieldValue<int>(CoreFieldRefNames.ExternalLinkCount); }
            set { SetFieldValue(CoreFieldRefNames.ExternalLinkCount, value); }
        }

        public string History
        {
            get { return GetFieldValue<string>(CoreFieldRefNames.History); }
            set { SetFieldValue(CoreFieldRefNames.History, value); }
        }

        public int HyperLinkCount
        {
            get { return GetFieldValue<int>(CoreFieldRefNames.HyperLinkCount); }
            set { SetFieldValue(CoreFieldRefNames.HyperLinkCount, value); }
        }

        public int IterationId
        {
            get { return GetFieldValue<int>(CoreFieldRefNames.IterationId); }
            set { SetFieldValue(CoreFieldRefNames.IterationId, value); }
        }

        public string IterationPath
        {
            get { return GetFieldValue<string>(CoreFieldRefNames.IterationPath); }
            set { SetFieldValue(CoreFieldRefNames.IterationPath, value); }
        }

        public string Reason
        {
            get { return GetFieldValue<string>(CoreFieldRefNames.Reason); }
            set { SetFieldValue(CoreFieldRefNames.Reason, value); }
        }

        public int RelatedLinkCount
        {
            get { return GetFieldValue<int>(CoreFieldRefNames.RelatedLinkCount); }
            set { SetFieldValue(CoreFieldRefNames.RelatedLinkCount, value); }
        }

        public DateTime? RevisedDate
        {
            get { return GetFieldValue<DateTime?>(CoreFieldRefNames.RevisedDate); }
            set { SetFieldValue(CoreFieldRefNames.RevisedDate, value); }
        }

        public DateTime? AuthorizedDate
        {
            get { return GetFieldValue<DateTime?>(CoreFieldRefNames.AuthorizedDate); }
            set { SetFieldValue(CoreFieldRefNames.AuthorizedDate, value); }
        }

        public string TeamProject
        {
            get { return GetFieldValue<string>(CoreFieldRefNames.TeamProject); }
            set { SetFieldValue(CoreFieldRefNames.TeamProject, value); }
        }

        public string Tags
        {
            get { return GetFieldValue<string>(CoreFieldRefNames.Tags); }
            set { SetFieldValue(CoreFieldRefNames.Tags, value); }
        }

        public string Title
        {
            get { return GetFieldValue<string>(CoreFieldRefNames.Title); }
            set { SetFieldValue(CoreFieldRefNames.Title, value); }
        }

        public double Watermark
        {
            get { return GetFieldValue<double>(CoreFieldRefNames.Watermark); }
            set { SetFieldValue(CoreFieldRefNames.Watermark, value); }
        }

        public bool IsDeleted { get; }

        public bool IsReadOnly { get; } = false;

        public bool IsNew => Id is TemporaryWorkItemId;

        public bool IsDirty { get; internal set; }

        internal RecycleStatus RecycleStatus { get; set; } = RecycleStatus.NoChange;

        internal JsonPatchDocument Changes { get; } = new JsonPatchDocument();

        public object this[string field]
        {
            get { return GetFieldValue<object>(field); }
            set { SetFieldValue(field, value); }
        }

        private void SetFieldValue(string field, object value)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException("Work item is read-only.");
            }

            if (_item.Fields.ContainsKey(field))
            {
                _item.Fields[field] = value;
                Changes.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Replace,
                    Path = "/fields/" + field,
                    Value = TranslateValue(value)
                });
            }
            else
            {
                _item.Fields.Add(field, value);
                Changes.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/" + field,
                    Value = TranslateValue(value)
                });
            }

            IsDirty = true;
        }

        private object TranslateValue(object value)
        {
            switch (value)
            {
                case IdentityRef id:
                {
                    return id.DisplayName;
                }
                default:
                {
                    return value;
                }
            }
        }

        private T GetFieldValue<T>(string field)
        {
            return _item.Fields.TryGetValue(field, out var value)
                ? (T)value
                : default(T);
        }

        internal void ReplaceIdAndResetChanges(int oldId, int newId)
        {
            if (oldId >= 0) throw new ArgumentOutOfRangeException(nameof(oldId));

            Id = new PermanentWorkItemId(newId);

            var candidates = Changes.Where(op => op.Path == "/relations/-");
            foreach (var op in candidates)
            {
                var patch = op.Value as RelationPatch;
                string url = patch.url;
                int pos = url.LastIndexOf('/') + 1;
                int relId = int.Parse(url.Substring(pos));
                if (relId == oldId)
                {
                    patch.url = url.Substring(0, pos) + newId.ToString();
                    break;
                }
            }

            Changes.RemoveAll(op => op.Path.StartsWith("/fields/", StringComparison.OrdinalIgnoreCase) || op.Path == "/id");
        }

        internal void RemapIdReferences(IDictionary<int, int> realIds)
        {
            var candidates = Changes.Where(op => op.Path == "/relations/-");
            foreach (var op in candidates)
            {
                var patch = op.Value as RelationPatch;
                string url = patch.url;
                int pos = url.LastIndexOf('/') + 1;
                int relId = int.Parse(url.Substring(pos));
                if (realIds.TryGetValue(relId, out var newId))
                {
                    string newUrl = url.Substring(0, pos) + newId.ToString();
                    patch.url = newUrl;
                }
            }
        }
    }

    internal enum RecycleStatus
    {
        NoChange,
        ToDelete,
        ToRestore,
    }
}
