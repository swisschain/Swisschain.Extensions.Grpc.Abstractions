using System;
using System.Collections.Generic;

namespace Swisschain.Extensions.Grpc.Abstractions
{
    public interface IStreamItemCollection<out TStreamItem, TId> 
        where TId : IComparable<TId>, IComparable
        where TStreamItem : IStreamItem<TId>
    {
        IReadOnlyCollection<TStreamItem> StreamItems { get; }
    }
}
