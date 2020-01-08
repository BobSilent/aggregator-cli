using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace aggregator.Engine
{
    public class WorkItemRelationWrapper : IEquatable<WorkItemRelationWrapper>
    {
        private readonly WorkItemRelation _relation;

        private WorkItemRelationWrapper(string relationUrl)
        {
            if (!string.IsNullOrWhiteSpace(relationUrl))
            {
                var relationUri = new Uri(relationUrl);
                var id = int.Parse(relationUri.Segments.Last());
                LinkedId = new PermanentWorkItemId(id);
            }
        }

        internal WorkItemRelationWrapper(WorkItemRelation relation) : this(relation.Url)
        {
            _relation = relation;
        }


        internal WorkItemRelationWrapper(string type, string url, string comment) : this(url)
        {
            _relation = new WorkItemRelation()
            {
                Rel = type,
                Url = url,
                Attributes = new Dictionary<string,object> { { "comment", comment } }
            };
        }

        public string Title => _relation.Title;

        public string Rel => _relation.Rel;

        public string Url => _relation.Url;

        public WorkItemId LinkedId { get; }

        public IDictionary<string, object> Attributes => _relation.Attributes;

        public bool Equals(WorkItemRelationWrapper other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _relation.Rel.Equals(other._relation.Rel) && _relation.Url.Equals(other._relation.Url);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is WorkItemRelationWrapper other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_relation.Rel.GetHashCode() * 397) ^ _relation.Url.GetHashCode();
            }
        }

        public static bool operator ==(WorkItemRelationWrapper left, WorkItemRelationWrapper right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(WorkItemRelationWrapper left, WorkItemRelationWrapper right)
        {
            return !Equals(left, right);
        }
    }
}
