using System;

namespace Core.Crud.Interfaces
{
    public interface ISafeRemovable
    {
        bool IsRemoved { get; set; }
        DateTime? RemovedOn { get; set; }
    }
}