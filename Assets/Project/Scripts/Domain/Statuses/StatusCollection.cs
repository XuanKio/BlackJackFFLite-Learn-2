using System.Collections.Generic;
using System.Linq;

namespace BlackJackFFLite.Domain.Statuses
{
    public sealed class StatusCollection
    {
        private readonly List<IStatus> _statuses = new List<IStatus>();

        public IReadOnlyList<IStatus> All => _statuses;

        public void Add(IStatus status)
        {
            IStatus existingStatus = _statuses.FirstOrDefault(item => item.Id == status.Id);

            if (existingStatus != null)
            {
                _statuses.Remove(existingStatus);
            }

            _statuses.Add(status);
        }

        public bool Has<T>() where T : IStatus
        {
            return _statuses.Any(status => status is T);
        }

        public T Get<T>() where T : class, IStatus
        {
            return _statuses.OfType<T>().FirstOrDefault();
        }

        public void TickAll()
        {
            foreach (IStatus status in _statuses)
            {
                status.Tick();
            }

            _statuses.RemoveAll(status => status.Duration <= 0);
        }

        public void Clear()
        {
            _statuses.Clear();
        }
    }
}