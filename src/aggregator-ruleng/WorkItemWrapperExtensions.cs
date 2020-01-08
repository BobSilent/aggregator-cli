using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;


namespace aggregator.Engine {
    internal static class WorkItemWrapperExtensions
    {
        public static bool HasAddsToNewWorkItems(this WorkItemRelationWrapperCollection relations)
        {
            return relations.Any(r => r.LinkedId is TemporaryWorkItemId);
        }

        public static IEnumerable<WorkItemId> GetWorkItemIdsToCreate(this WorkItemRelationWrapperCollection relations)
        {
            return relations.Where(r => r.LinkedId is TemporaryWorkItemId)
                            .Select(r => r.LinkedId);
        }

        public static IEnumerable<WorkItemId> GetRelatedWorkItemsWhichNeedsToBeCreated(this WorkItemWrapper item)
        {
            return item.Relations.GetWorkItemIdsToCreate();
        }

        public static IEnumerable<WorkItemWrapper> WhereUpdateNeeded(this IEnumerable<WorkItemWrapper> workItems)
        {
            return workItems.Where(WorkItem => WorkItem.IsDirty);
        }

        public static JsonPatchDocument GetChangesAsPatchDocument(this WorkItemWrapper item, bool withoutRelationChanges = false, bool ensureRevision = true)
        {
            var document = new JsonPatchDocument();
            document.AddRange(item.GetChanges(withoutRelationChanges: withoutRelationChanges, ensureRevision: ensureRevision));

            return document;
        }

        private static IEnumerable<JsonPatchOperation> GetChanges(this WorkItemWrapper item, bool withoutRelationChanges = false, bool ensureRevision = true)
        {
            if (ensureRevision && item.Id is PermanentWorkItemId)
            {
                yield return new JsonPatchOperation()
                             {
                                 Operation = Operation.Test,
                                 Path = "/rev",
                                 Value = item.Rev
                             };
            }

            foreach (KeyValuePair<string, (object newValue, Operation operation)> field in item.FieldChanges)
            {
                var path = $"/fields/{field.Key}";
                var (newValue, operation) = field.Value;

                yield return new JsonPatchOperation()
                             {
                                 Operation = operation,
                                 Path = path,
                                 Value = TranslateValue(newValue)
                             };
            }

            if (withoutRelationChanges)
            {
                yield break;
            }

            foreach (var relationChange in item.Relations.GetChanges())
            {
                var relation = relationChange.relation;
                var patch = new JsonPatchOperation()
                            {
                                Operation = relationChange.operation,
                                Path = "/relations/-"
                            };

                if (relationChange.operation == Operation.Add)
                {
                    patch.Value = new RelationPatch
                                  {
                                      rel = relation.Rel,
                                      url = relation.Url,
                                      attributes = relation.Attributes != null &&
                                                   relation.Attributes.TryGetValue("comment", out var value)
                                                       ? new {comment = value}
                                                       : null
                                  };
                }
                else if (relationChange.operation == Operation.Remove)
                {
                    patch.Value = relationChange.relationIndex;
                }

                yield return patch;
            }
        }

        private static object TranslateValue(object value)
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

    }
}