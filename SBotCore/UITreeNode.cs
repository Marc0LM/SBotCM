using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;


namespace SBotCore
{
    public class UITreeNode
    {
        public ulong python_object_address;

        public string python_object_type_name;

        public DictEntriesOfInterest dict_entries_of_interest;

        public class DictEntriesOfInterest : Dictionary<string, object>
        {

            public T Value<T>(string key)
            {
                if (this.ContainsKey(key) && this[key].GetType() == typeof(T))
                {
                    return (T)this[key];
                }
                else
                {
                    return default(T);
                }
            }
        }

        public string[] other_dict_entries_keys;

        public UITreeNode[] children;

        public struct DictEntryValueGenericRepresentation
        {
            public ulong address_;

            public string python_object_type_name;
        }

        public struct DictEntry
        {
            public string key;
            public object value;
        }

        public class Bunch
        {
            public List<DictEntry> entries_of_interest;
        }

        public IEnumerable<UITreeNode> EnumerateSelfAndDescendants() =>
            new[] { this }
            .Concat((children ?? Array.Empty<UITreeNode>()).SelectMany(child => child?.EnumerateSelfAndDescendants() ?? ImmutableList<UITreeNode>.Empty));

        public UITreeNode WithOtherDictEntriesRemoved()
        {
            return new UITreeNode
            {
                python_object_address = python_object_address,
                python_object_type_name = python_object_type_name,
                dict_entries_of_interest = dict_entries_of_interest,
                other_dict_entries_keys = null,
                children = children?.Select(child => child?.WithOtherDictEntriesRemoved()).ToArray(),
            };
        }
    }
}
