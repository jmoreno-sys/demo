using Dominio.Interfaces;
using System;

namespace Dominio.Abstracciones
{
    public abstract class Entidad : Entidad<int>
    {
    }
    public abstract class Entidad<TKey> : IEntidad<TKey> where TKey : struct, IComparable<TKey>
    {
        public TKey Id { get; set; }

        public override string ToString()
        {
            return $"[{Id}] - {base.ToString()}";
        }
    }
}

