using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace aggregator.Engine
{
    public class WorkItemRelationWrapperCollection : ICollection<WorkItemRelationWrapper>
    {
        private IList<WorkItemRelationWrapper> _original;
        private HashSet<WorkItemRelationWrapper> _current;
        private IDictionary<WorkItemRelationWrapper, Operation> _changes;

        internal WorkItemRelationWrapperCollection(WorkItemWrapper workItem, IList<WorkItemRelation> relations)
        {
            IsReadOnly = workItem.IsReadOnly;
            _original = relations == null
                ? new List<WorkItemRelationWrapper>()
                : relations.Select(relation => new WorkItemRelationWrapper(relation))
                           .ToList();

            // do we need deep cloning?
            _current = new HashSet<WorkItemRelationWrapper>(_original);
            _changes = new Dictionary<WorkItemRelationWrapper, Operation>();
        }

        private void AddRelation(WorkItemRelationWrapper item)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException("Work item is read-only.");
            }

            if (_original.Contains(item))
            {
                _changes.Remove(item);
                return;
            }

            _changes[item] = Operation.Add;
            _current.Add(item);
        }

        private bool RemoveRelation(WorkItemRelationWrapper item)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException("Work item is read-only.");
            }

            if (!_original.Contains(item))
            {
                _changes.Remove(item);
                return true;
            }

            _changes[item] = Operation.Remove;
            return _current.Remove(item);
        }

        internal IEnumerable<(WorkItemRelationWrapper relation, int relationIndex, Operation operation)> GetChanges()
        {
            var added   = _current.Except(_original)
                                  .Select(item => (relation: item, relationIndex: int.MaxValue, operation: Operation.Add));
            var removed = _original.Except(_current)
                                   .Select(item => (relation: item, relationIndex: _original.IndexOf(item), operation: Operation.Remove));

            return removed.Concat(added);
        }

        public IEnumerator<WorkItemRelationWrapper> GetEnumerator()
        {
            return _current.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_current).GetEnumerator();
        }

        public void Add(WorkItemRelationWrapper item)
        {
            AddRelation(item);
        }

        public void AddChild(WorkItemWrapper child)
        {
            var r = new WorkItemRelationWrapper(CoreRelationRefNames.Children, child.Url, string.Empty);
            AddRelation(r);
        }

        public void AddParent(WorkItemWrapper parent)
        {
            var r = new WorkItemRelationWrapper(CoreRelationRefNames.Parent, parent.Url, string.Empty);
            AddRelation(r);
        }

        public void AddLink(string type, string url, string comment)
        {
            AddRelation(new WorkItemRelationWrapper(
                type,
                url,
                comment
            ));
        }


        public void AddHyperlink(string url, string comment = null)
        {
            AddLink(
                CoreRelationRefNames.Hyperlink,
                url,
                comment
            );
        }

        public void AddRelatedLink(WorkItemWrapper item, string comment = null)
        {
            AddRelatedLink(item.Url, comment);
        }


        public void AddRelatedLink(string url, string comment = null)
        {
            AddLink(
                CoreRelationRefNames.Related,
                url,
                comment
            );
        }

        public void Clear()
        {
            foreach (var item in _current.ToArray())
            {
                RemoveRelation(item);
            }
        }

        public bool Contains(WorkItemRelationWrapper item)
        {
            return _current.Contains(item);
        }

        public void CopyTo(WorkItemRelationWrapper[] array, int arrayIndex)
        {
            _current.CopyTo(array, arrayIndex);
        }

        public bool Remove(WorkItemRelationWrapper item)
        {
            return RemoveRelation(item);
        }

        public int Count => _current.Count;

        public bool IsReadOnly { get; }

        public bool IsDirty => !_current.SetEquals(_original);
    }
}
