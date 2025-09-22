using System.Threading;
using System.Threading.Tasks;

namespace com.aoyon.AutoConfigureTexture
{
    public class ThreadHelper
    {
        private static SynchronizationContext _mainThreadContext;
        private static int _mainThreadId;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            _mainThreadContext = SynchronizationContext.Current;
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public static bool IsMainThread()
        {
            return Thread.CurrentThread.ManagedThreadId == _mainThreadId;
        }

        public static void ExecuteOnMainThread(Action action)
        {
            if (IsMainThread())
            {
                action();
                return;
            }

            _mainThreadContext.Send(_ =>
            {
                action();
            }, null);
        }

        public static T ExecuteOnMainThread<T>(Func<T> action)
        {
            if (IsMainThread())
            {
                return action();
            }

            T result = default;
            _mainThreadContext.Send(_ =>
            {
                result = action();
            }, null);

            return result;
        }

        public static void ExecuteOnMainThread(Func<Task> asyncAction)
        {
            _mainThreadContext.Send(async _ =>
            {
                await asyncAction();
            }, null);
        }

        public static T ExecuteOnMainThread<T>(Func<Task<T>> asyncAction)
        {
            T result = default;
            _mainThreadContext.Send(async _ =>
            {
                result = await asyncAction();
            }, null);

            return result;
        }
    }
}