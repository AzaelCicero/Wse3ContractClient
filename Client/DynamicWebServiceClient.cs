using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using ImpromptuInterface;

namespace Wse3ContractClient.Client
{
    internal class DynamicWebServiceClient : ImpromptuInterface.Dynamic.ImpromptuObject
    {
        const string AsyncEnd = "Async";
        private readonly IWse3GenericClient _client;

        public DynamicWebServiceClient(IWse3GenericClient client)
        {
            _client = client;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            var action = binder.Name;

            if (action.EndsWith(AsyncEnd))
            {
                var actionWithoutAsync = action.Remove(action.Length - AsyncEnd.Length, AsyncEnd.Length);
                var methodInfo = KnownInterfaces.Select(i => i.GetMethod(action)).Single(m => m != null);
                var paramInfos = methodInfo.GetParameters().ToList();
                var taskType = methodInfo.ReturnType;
                var webMethodReturnType = KnownInterfaces.First().GetInterfaces().Select(i => i.GetMethod(actionWithoutAsync)).Single(m => m != null).ReturnType;

                var argTypes = PrepareArgs(args, paramInfos);

                var task = CreateTask(
                    () => CallWebService(actionWithoutAsync, args, paramInfos.Select(info => info.Name).ToList(), argTypes, webMethodReturnType), 
                    taskType, 
                    webMethodReturnType);
                task.Start();

                result = task;
            }
            else
            {
                var methodInfo = KnownInterfaces.Select(i => i.GetMethod(action)).FirstOrDefault(m => m != null) ??
                    KnownInterfaces.First().GetInterfaces().Select(i => i.GetMethod(action)).Single(m => m != null);
                var paramInfos = methodInfo.GetParameters().ToList();

                var realReturnType = methodInfo.ReturnType;

                var argTypes = PrepareArgs(args, paramInfos);

                result = CallWebService(action, args, paramInfos.Select(info => info.Name).ToList(), argTypes, realReturnType);
            }

            return true;
        }

        private IList<Type> PrepareArgs(object[] args, List<ParameterInfo> paramInfos)
        {
            var argsTypes = new List<Type>(args.Length);

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg is IEnumerable && !arg.GetType().IsArray && !(arg is string))
                {
                    var casted = (arg as IEnumerable);
                    var length = casted.Cast<object>().Count();

                    var arrayOfGoodTypes = Array.CreateInstance(casted.GetType().GetGenericArguments()[0], length);
                    Array.Copy(casted.Cast<object>().ToArray(), arrayOfGoodTypes, length);
                    
                    args[i] = arrayOfGoodTypes;
                    argsTypes.Add(arrayOfGoodTypes.GetType());
                    continue;
                }
                argsTypes.Add(paramInfos[i].ParameterType);
            }

            return argsTypes;
        }

        private Type AdjustTypeForWse(Type toAdjust)
        {
            if (toAdjust.IsArray || !typeof(IEnumerable).IsAssignableFrom(toAdjust))
            {
                return toAdjust;
            }

            var solid = Array.CreateInstance(toAdjust.GetGenericArguments()[0], 0);
            return solid.GetType();
        }

        private static Task CreateTask(Func<object> func, Type taskType, Type webMethodReturnType)
        {
            if (webMethodReturnType == typeof(void))
            {
                return new Task(() => func());
            }

            var call = Expression.Convert(
                func.Target == null ? Expression.Call(func.Method) 
                                    : Expression.Call(Expression.Constant(func.Target), func.Method), webMethodReturnType);

            var delegateType = Expression.Lambda(typeof(Func<>).MakeGenericType(webMethodReturnType), call, null).Compile();
            var task = Impromptu.InvokeConstructor(taskType, delegateType) as Task;

            return task;
        }

        private object CallWebService(string action, object[] args, List<string> paramNames, IList<Type> paramTypes, Type returnType)
        {
            var adjustedWseType = AdjustTypeForWse(returnType);

            var result = _client.Invoke(action, args, paramNames, paramTypes, adjustedWseType);

            if (adjustedWseType == returnType)
            {
                return result;
            }

            return ConvertCollection(result, returnType);
        }

        private object ConvertCollection(object result, Type returnType)
        {
            var casted = ConvertArray(result, returnType.GetGenericArguments()[0]);

            return Impromptu.InvokeConstructor(returnType, casted);
        }

        private static object ConvertArray(object obj, Type targetType)
        {
            if (obj is Array)
            {
                var convertMethod = typeof(DynamicWebServiceClient).GetMethod(
                    "ConvertArrayTemplate",
                    BindingFlags.NonPublic | BindingFlags.Static);
                var generic = convertMethod.MakeGenericMethod(targetType);

                return generic.Invoke(null, new[] { obj });
            }
            return obj;
        }

        private static IEnumerable<T> ConvertArrayTemplate<T>(Array input)
        {
            return input.Cast<T>();
        }
    }
}