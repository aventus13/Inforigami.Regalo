using System;

namespace Inforigami.Regalo.Core
{
    [Obsolete("Please use ICommandProcessor instead", true)]
    public interface ICommandBus
    {
        void Send<TCommand>(TCommand command);
    }
}
