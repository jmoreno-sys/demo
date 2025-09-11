using System;

namespace Dominio.Interfaces
{
    public interface IEntidad : IEntidad<int>
    {
    }

    public interface IEntidad<TKey> where TKey : struct, IComparable<TKey>
    {
        public TKey Id { get; set; }
    }
}
