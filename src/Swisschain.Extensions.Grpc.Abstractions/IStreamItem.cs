using System;

namespace Swisschain.Extensions.Grpc.Abstractions
{
    public interface IStreamItem<TId> where TId : IComparable<TId>, IComparable
    {
        TId StreamItemId { get; }
    }
}

