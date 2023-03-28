using System;

namespace EveSde
{
    public interface IEveSdeClient : IDisposable
    {
        bool Start();
        string GetName(uint id);
    }
}