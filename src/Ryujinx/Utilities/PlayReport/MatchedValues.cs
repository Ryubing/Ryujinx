using MsgPack;
using Ryujinx.Ava.Utilities.AppLibrary;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Ava.Utilities.PlayReport
{
    public abstract class MatchedValue<T>
    {
        public MatchedValue(T matched)
        {
            Matched = matched;
        }
        
        /// <summary>
        /// The currently running application's <see cref="ApplicationMetadata"/>.
        /// </summary>
        public ApplicationMetadata Application { get; init; }
        
        /// <summary>
        /// The entire play report.
        /// </summary>
        public Horizon.Prepo.Types.PlayReport PlayReport { get; init; }

        /// <summary>
        /// The matched value from the Play Report.
        /// </summary>
        public T Matched { get; init; }
    }
    
    /// <summary>
    /// The input data to a <see cref="ValueFormatter"/>,
    /// containing the currently running application's <see cref="ApplicationMetadata"/>,
    /// and the matched <see cref="MessagePackObject"/> from the Play Report.
    /// </summary>
    public class SingleValue : MatchedValue<Value>
    {
        public SingleValue(Value matched) : base(matched)
        {
        }

        public static implicit operator SingleValue(MessagePackObject mpo) => new(mpo);
    }

    /// <summary>
    /// The input data to a <see cref="MultiValueFormatter"/>,
    /// containing the currently running application's <see cref="ApplicationMetadata"/>,
    /// and the matched <see cref="MessagePackObject"/>s from the Play Report.
    /// </summary>
    public class MultiValue : MatchedValue<Value[]>
    {
        public MultiValue(Value[] matched) : base(matched)
        {
        }

        public MultiValue(IEnumerable<MessagePackObject> matched) : base(Value.ConvertPackedObjects(matched))
        {
        }

        public static implicit operator MultiValue(List<MessagePackObject> matched)
            => new(matched.Select(x => new Value(x)).ToArray());
    }

    /// <summary>
    /// The input data to a <see cref="SparseMultiValueFormatter"/>,
    /// containing the currently running application's <see cref="ApplicationMetadata"/>,
    /// and the matched <see cref="MessagePackObject"/>s from the Play Report.
    /// </summary>
    public class SparseMultiValue : MatchedValue<Dictionary<string, Value>>
    {
        public SparseMultiValue(Dictionary<string, Value> matched) : base(matched)
        {
        }
        
        public SparseMultiValue(Dictionary<string, MessagePackObject> matched) : base(Value.ConvertPackedObjectMap(matched))
        {
        }

        public static implicit operator SparseMultiValue(Dictionary<string, MessagePackObject> matched)
            => new(matched
                .ToDictionary(
                    x => x.Key,
                    x => new Value(x.Value)
                )
            );
    }
}
