using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;

using System;
using System.Collections.Generic;
using System.Linq;

namespace aggregator.Engine
{
    public class WorkItemWrapper
    {
        private readonly EngineContext _context;
        private readonly WorkItem _item;
        private RecycleStatus _recycleStatus;

        internal WorkItemWrapper(EngineContext context, WorkItem item)
        {
            _context = context;
            _item = item;
            _recycleStatus = RecycleStatus.NoChange;
            Relations = new WorkItemRelationWrapperCollection(this, _item.Relations);

            if (item.Id.HasValue)
            {
                Id = new PermanentWorkItemId(item.Id.Value);
                //for simplify testing: item.Url can be null
                IsDeleted = item.Url?.EndsWith($"/recyclebin/{item.Id.Value}", StringComparison.OrdinalIgnoreCase) ?? false;

                IsReadOnly = false;
                _context.Tracker.TrackExisting(this);
            }
            else
            {
                Id = new TemporaryWorkItemId(_context.Tracker);

                _context.Tracker.TrackNew(this);
            }
        }


        internal WorkItemWrapper(EngineContext context, WorkItem item, bool isReadOnly)
        // we cannot reuse the code, because tracking is different
        //: this(context, item)
        {
            _context = context;
            _item = item;
            _recycleStatus = RecycleStatus.NoChange;
            Relations = new WorkItemRelationWrapperCollection(this, _item.Relations);

            Id = new PermanentWorkItemId(item.Id.Value);
            IsDeleted = item.Url?.EndsWith($"/recyclebin/{item.Id}", StringComparison.OrdinalIgnoreCase) ?? false;

            IsReadOnly = isReadOnly;
            _context.Tracker.TrackRevision(this);
        }

        public WorkItemWrapper PreviousRevision
        {
            get
            {
                if (Rev > 0)
                {
                    // TODO we shouldn't use the client in this class, move to WorkItemStore.GetRevisionAsync, workitemstore should check tracker if already loaded
                    // TODO think about passing workitemstore into workitemwrapper constructor, instead of engineContext, workitemstore is used several times, see also Property Children/Parent
                    var previousRevision = _context.Clients.WitClient.GetRevisionAsync(this.Id, this.Rev - 1, expand: WorkItemExpand.All).Result;
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
                //var all = _context.Clients.WitClient.GetRevisionsAsync(this.Id, expand: WorkItemExpand.All).Result;
                var revision = this;
                while ((revision = revision.PreviousRevision) != null)
                {
                    yield return revision;
                }
            }
        }

        public IEnumerable<WorkItemRelationWrapper> RelationLinks => Relations;

        public WorkItemRelationWrapperCollection Relations { get; }

        public IEnumerable<WorkItemRelationWrapper> ChildrenLinks
        {
            get
            {
                return Relations
                    .Where(rel => rel.Rel == CoreRelationRefNames.Children);
            }
        }

        public IEnumerable<WorkItemWrapper> Children
        {
            get
            {
                if (ChildrenLinks != null && ChildrenLinks.Any())
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
                return Relations
                    .Where(rel => rel.Rel == CoreRelationRefNames.Related);
            }
        }

        public IEnumerable<WorkItemRelationWrapper> Hyperlinks
        {
            get
            {
                return Relations
                    .Where(rel => rel.Rel == CoreRelationRefNames.Hyperlink);
            }
        }

        public WorkItemRelationWrapper ParentLink
        {
            get
            {
                return Relations
                    .SingleOrDefault(rel => rel.Rel == CoreRelationRefNames.Parent);
            }
        }

        public WorkItemWrapper Parent
        {
            get
            {
                if (ParentLink != null)
                {
                    var store = new WorkItemStore(_context);
                    return store.GetWorkItem(ParentLink);
                }
                else
                    return null;
            }
        }

        public WorkItemId Id
        {
            get;
            private set;
        }

        public int Rev => _item.Rev.Value;

        public string Url => _item.Url;

        public string WorkItemType
        {
            get => (string)_item.Fields[CoreFieldRefNames.WorkItemType];
            private set => SetFieldValue(CoreFieldRefNames.WorkItemType, value);
        }

        public string State
        {
            get => GetFieldValue<string>(CoreFieldRefNames.State);
            set => SetFieldValue(CoreFieldRefNames.State, value);
        }

        public int AreaId
        {
            get => GetFieldValue<int>(CoreFieldRefNames.AreaId);
            set => SetFieldValue(CoreFieldRefNames.AreaId, value);
        }

        public string AreaPath
        {
            get => GetFieldValue<string>(CoreFieldRefNames.AreaPath);
            set => SetFieldValue(CoreFieldRefNames.AreaPath, value);
        }

        public IdentityRef AssignedTo
        {
            get => GetFieldValue<IdentityRef>(CoreFieldRefNames.AssignedTo);
            set => SetFieldValue(CoreFieldRefNames.AssignedTo, value);
        }

        public int AttachedFileCount
        {
            get => GetFieldValue<int>(CoreFieldRefNames.AttachedFileCount);
            set => SetFieldValue(CoreFieldRefNames.AttachedFileCount, value);
        }

        public IdentityRef AuthorizedAs
        {
            get => GetFieldValue<IdentityRef>(CoreFieldRefNames.AuthorizedAs);
            set => SetFieldValue(CoreFieldRefNames.AuthorizedAs, value);
        }

        public IdentityRef ChangedBy
        {
            get => GetFieldValue<IdentityRef>(CoreFieldRefNames.ChangedBy);
            set => SetFieldValue(CoreFieldRefNames.ChangedBy, value);
        }

        public DateTime? ChangedDate
        {
            get => GetFieldValue<DateTime?>(CoreFieldRefNames.ChangedDate);
            set => SetFieldValue(CoreFieldRefNames.ChangedDate, value);
        }

        public IdentityRef CreatedBy
        {
            get => GetFieldValue<IdentityRef>(CoreFieldRefNames.CreatedBy);
            set => SetFieldValue(CoreFieldRefNames.CreatedBy, value);
        }

        public DateTime? CreatedDate
        {
            get => GetFieldValue<DateTime?>(CoreFieldRefNames.CreatedDate);
            set => SetFieldValue(CoreFieldRefNames.CreatedDate, value);
        }

        public string Description
        {
            get => GetFieldValue<string>(CoreFieldRefNames.Description);
            set => SetFieldValue(CoreFieldRefNames.Description, value);
        }

        public int ExternalLinkCount
        {
            get => GetFieldValue<int>(CoreFieldRefNames.ExternalLinkCount);
            set => SetFieldValue(CoreFieldRefNames.ExternalLinkCount, value);
        }

        public string History
        {
            get => GetFieldValue<string>(CoreFieldRefNames.History);
            set => SetFieldValue(CoreFieldRefNames.History, value);
        }

        public int HyperLinkCount
        {
            get => GetFieldValue<int>(CoreFieldRefNames.HyperLinkCount);
            set => SetFieldValue(CoreFieldRefNames.HyperLinkCount, value);
        }

        public int IterationId
        {
            get => GetFieldValue<int>(CoreFieldRefNames.IterationId);
            set => SetFieldValue(CoreFieldRefNames.IterationId, value);
        }

        public string IterationPath
        {
            get => GetFieldValue<string>(CoreFieldRefNames.IterationPath);
            set => SetFieldValue(CoreFieldRefNames.IterationPath, value);
        }

        public string Reason
        {
            get => GetFieldValue<string>(CoreFieldRefNames.Reason);
            set => SetFieldValue(CoreFieldRefNames.Reason, value);
        }

        public int RelatedLinkCount
        {
            get => GetFieldValue<int>(CoreFieldRefNames.RelatedLinkCount);
            set => SetFieldValue(CoreFieldRefNames.RelatedLinkCount, value);
        }

        public IdentityRef RevisedBy
        {
            get => GetFieldValue<IdentityRef>(CoreFieldRefNames.RevisedBy);
            set => SetFieldValue(CoreFieldRefNames.RevisedBy, value);
        }

        public DateTime? RevisedDate
        {
            get => GetFieldValue<DateTime?>(CoreFieldRefNames.RevisedDate);
            set => SetFieldValue(CoreFieldRefNames.RevisedDate, value);
        }

        public DateTime? AuthorizedDate
        {
            get => GetFieldValue<DateTime?>(CoreFieldRefNames.AuthorizedDate);
            set => SetFieldValue(CoreFieldRefNames.AuthorizedDate, value);
        }

        public string TeamProject
        {
            get => GetFieldValue<string>(CoreFieldRefNames.TeamProject);
            set => SetFieldValue(CoreFieldRefNames.TeamProject, value);
        }

        public string Tags
        {
            get => GetFieldValue<string>(CoreFieldRefNames.Tags);
            set => SetFieldValue(CoreFieldRefNames.Tags, value);
        }

        public string Title
        {
            get => GetFieldValue<string>(CoreFieldRefNames.Title);
            set => SetFieldValue(CoreFieldRefNames.Title, value);
        }

        public double Watermark
        {
            get => GetFieldValue<double>(CoreFieldRefNames.Watermark);
            set => SetFieldValue(CoreFieldRefNames.Watermark, value);
        }

        public bool IsDeleted { get; }

        public bool IsReadOnly { get; } = false;

        public bool IsNew => Id is TemporaryWorkItemId;

        public bool IsDirty => FieldChanges.Any() || RecycleStatus != RecycleStatus.NoChange || Relations.IsDirty;

        internal RecycleStatus RecycleStatus
        {
            get => _recycleStatus;
            set
            {
                if ((value == RecycleStatus.ToDelete && IsDeleted) ||
                    (value == RecycleStatus.ToRestore && !IsDeleted))
                {
                    // setting original value means in sum no change
                    _recycleStatus = RecycleStatus.NoChange;
                }
                else
                {
                    _recycleStatus = value;
                }
            }
        }

        internal IDictionary<string, (object value, Operation operation)> FieldChanges { get; } = new Dictionary<string, (object, Operation)>();

        internal IDictionary<string, object> RelationChanges { get; } = new Dictionary<string, object>();

        public object this[string field]
        {
            get => GetFieldValue<object>(field);
            set => SetFieldValue(field, value);
        }

        private void SetFieldValue(string field, object value)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException("Work item is read-only.");
            }

            var operation = Operation.Add;

            if (_item.Fields.ContainsKey(field))
            {
                if (_item.Fields[field].Equals(value))
                {
                    // if new value does not differ from existing value, just ignore change
                    FieldChanges.Remove(field);
                    return;
                }

                operation = Operation.Replace;
                FieldChanges[field] = (value, operation);
            }
            else if (value == default)
            {
                //was added, should now be deleted, so in sum no change
                FieldChanges.Remove(field);
            }
            else
            {
                FieldChanges[field] = (value, operation);
            }
        }

        public T GetFieldValue<T>(string field, T defaultValue = default)
        {
            var isChangedValue = FieldChanges.TryGetValue(field, out var updatedItem);
            var hasFieldValue = _item.Fields.TryGetValue(field, out var originalValue);

            // prefer already changed value over original Value
            var value = isChangedValue ? updatedItem.value : originalValue;

            return hasFieldValue
                ? (T)Convert.ChangeType(value, typeof(T))
                : defaultValue;
        }

        /// <summary>
        /// when new work item was created, set the correct work item id after creation
        /// and if already field values where added persist them
        /// </summary>
        /// <param name="newId">the Id from created WorkItem</param>
        /// <param name="clearFieldChanges"></param>
        internal void SetPermanentId(int newId, bool clearFieldChanges = true)
        {
            if (Id is PermanentWorkItemId)
            {
                throw new ArgumentException("Work Item already has a valid Id");
            }

            Id = new PermanentWorkItemId(newId);

            if (!clearFieldChanges)
            {
                return;
            }

            //persist field changes
            foreach (var fieldChange in FieldChanges)
            {
                var fieldKey = fieldChange.Key;
                (object value, Operation operation) = fieldChange.Value;

                switch (operation) {
                    case Operation.Add:
                    case Operation.Replace:
                        _item.Fields[fieldKey] = value;
                        break;

                    case Operation.Remove:
                        _item.Fields.Remove(fieldKey);
                        break;
                }
            }
            FieldChanges.Clear();
        }
    }

    internal enum RecycleStatus
    {
        NoChange,
        ToDelete,
        ToRestore,
    }
}
