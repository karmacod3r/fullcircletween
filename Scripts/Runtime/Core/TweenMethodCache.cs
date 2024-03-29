using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FullCircleTween.Attributes;
using FullCircleTween.Core.Interfaces;
using FullCircleTween.Properties;
using UnityEngine;
using Object = System.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FullCircleTween.Core
{
    public static class TweenMethodCache
    {
        public static List<string> allComponentTypeNames = new List<string>();
        private static Dictionary<Type, Dictionary<string, MethodInfo>> tweenMethods = new Dictionary<Type, Dictionary<string, MethodInfo>>();
        private static Dictionary<Type, Dictionary<string, MethodInfo>> tweenMethodsByPropertyPath = new Dictionary<Type, Dictionary<string, MethodInfo>>();
        private static Dictionary<Type, List<string>> popupListCache = new Dictionary<Type, List<string>>();

        private static readonly Dictionary<Type, Type> unityTypeMap = new Dictionary<Type, Type>
        {
            {typeof(Single), typeof(float)}
        };


#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void EditorInitialize()
        {
            if (Application.isPlaying) return;
            RecacheTweenMethods();
        }
#endif

        [RuntimeInitializeOnLoadMethod]
        private static void RuntimeInitialize()
        {
            if (!Application.isPlaying) return;
            RecacheTweenMethods();
        }

        public static void RecacheTweenMethods()
        {
            tweenMethods.Clear();
            popupListCache.Clear();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            assemblies.ForEach(assembly =>
            {
                assembly.GetTypes()
                    .Where(type => type.GetCustomAttribute<TweenCollectionAttribute>() != null)
                    .ToList()
                    .ForEach(AddTweenMethods);
            });

            foreach (var type in tweenMethods.Keys)
            {
                FillInBaseTypes(type);
            }
            
            allComponentTypeNames = tweenMethods.Keys.ToList()
                .Where(type => typeof(Component).IsAssignableFrom(type))
                .Select(type => type.AssemblyQualifiedName)
                .ToList();
            allComponentTypeNames.Sort();
        }

        private static void FillInBaseTypes(Type type)
        {
            var baseType = type.BaseType;
            while (baseType != null)
            {
                if (tweenMethods.ContainsKey(baseType))
                {
                    foreach (var entry in tweenMethods[baseType])
                    {
                        tweenMethods[type].TryAdd(entry.Key, entry.Value);
                    }
                    foreach (var entry in tweenMethodsByPropertyPath[baseType])
                    {
                        tweenMethodsByPropertyPath[type].TryAdd(entry.Key, entry.Value);
                    }
                }
                baseType = baseType.BaseType;
            }
        }

        private static void AddTweenMethods(Type extensionClass)
        {
            extensionClass.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList().ForEach(info =>
            {
                if (!typeof(ITween).IsAssignableFrom(info.ReturnType)) return;

                var parameters = info.GetParameters();
                var targetType = info.IsStatic ? parameters[0].ParameterType : extensionClass;

                if (!tweenMethods.ContainsKey(targetType))
                {
                    tweenMethods.Add(targetType, new Dictionary<string, MethodInfo>());
                }

                if (!tweenMethodsByPropertyPath.ContainsKey(targetType))
                {
                    tweenMethodsByPropertyPath.Add(targetType, new Dictionary<string, MethodInfo>());
                }

                var methodName = GetMethodName(info);
                if (tweenMethods[targetType].ContainsKey(methodName))
                {
                    Debug.LogError($"Tween method already defined - skipping {methodName} ({info}) for type {targetType}");
                    return;
                }

                tweenMethods[targetType][methodName] = info;

                var propertyPath = info.GetCustomAttribute<TweenPropertyPathAttribute>()?.propertyPath;
                if (! string.IsNullOrEmpty(propertyPath))
                {
                    tweenMethodsByPropertyPath[targetType][GetFirstLevelPropertyPath(propertyPath)] = info;
                }
            });
        }

        public static string GetFirstLevelPropertyPath(string propertyPath)
        {
            var dotIndex = propertyPath.IndexOf(".", StringComparison.Ordinal);
            if (dotIndex > -1)
            {
                propertyPath = propertyPath.Substring(0, dotIndex);
            }

            return propertyPath;
        }

        private static string GetFriendlyTypeName(Type type)
        {
            var ret = type.ToString();
            
            ret = ret switch
            {
                "System.Single" => "float",
                "System.Int32" => "int",
                "System.Bool" => "bool",
                "System.String" => "string",
                _ => ret
            };

            return ret.StartsWith("UnityEngine.") ? ret.Substring(12) : ret;
        }

        private static string GetMethodTweenedTypeName(MethodInfo info)
        {
            return GetFriendlyTypeName(GetMethodTweenedType(info));
        }

        /*
        public static string ExpandMethodName(string memberName, Type tweenedType)
        {
            
        }
        */

        public static string GetMethodName(MethodInfo info)
        {
            return info.Name + "(" + GetMethodTweenedTypeName(info) + ")";
        }

        public static Type GetMethodTweenedType(MethodInfo info)
        {
            if (info == null) return null;

            var parameterType = info.GetParameters()[info.IsStatic ? 1 : 0].ParameterType;
            return unityTypeMap.ContainsKey(parameterType) ? unityTypeMap[parameterType] : parameterType;
        }

        private static void EnsureTypeInitialization(Type targetType)
        {
            if (!tweenMethodsByPropertyPath.ContainsKey(targetType)) {
                tweenMethodsByPropertyPath.Add(targetType, new());
            }
            if (!tweenMethods.ContainsKey(targetType))
            {
                tweenMethods.Add(targetType, new());
                
                FillInBaseTypes(targetType);
            }
        }

        public static List<string> GetPopupMethodNames(Type targetType)
        {
            if (targetType == null) return new List<string>();
            if (popupListCache.ContainsKey(targetType)) return popupListCache[targetType];

            EnsureTypeInitialization(targetType);

            var ret = tweenMethods[targetType].Keys.ToList();
            ret.Sort();
            popupListCache[targetType] = ret;
            return ret;
        }

        public static MethodInfo GetTweenMethodInfoForPropertyPath(Type targetType, string propertyPath)
        {
            if (targetType == null) return null;
            EnsureTypeInitialization(targetType);
            
            if (tweenMethodsByPropertyPath[targetType].ContainsKey(propertyPath))
            {
                return tweenMethodsByPropertyPath[targetType][propertyPath];
            }

            return null;
        }

        public static MethodInfo GetTweenMethodInfo(Type targetType, string methodName)
        {
            if (targetType == null) return null;
            EnsureTypeInitialization(targetType);
            
            if (tweenMethods[targetType].ContainsKey(methodName))
            {
                return tweenMethods[targetType][methodName];
            }

            return null;
        }

        public static ITween CreateTween(object target, string methodName, TweenClipValue toValue, float duration)
        {
            var methodInfo = GetTweenMethodInfo(target.GetType(), methodName);
            var tweenedType = GetMethodTweenedType(methodInfo);

            return CreateTween(target, methodName, toValue.GetValue(tweenedType), duration);
        }

        public static ITween CreateTween(object target, string methodName, object toValue, float duration)
        {
            var methodInfo = GetTweenMethodInfo(target.GetType(), methodName);
            if (methodInfo == null) return null;

            var tweenedType = GetMethodTweenedType(methodInfo);

            var parameters = methodInfo.GetParameters();
            if (methodInfo.IsStatic && parameters.Length == 3)
            {
                return (ITween) methodInfo.Invoke(target, new[] {target, toValue, duration});
            }

            if (!methodInfo.IsStatic && parameters.Length == 2)
            {
                return (ITween) methodInfo.Invoke(target, new[] {toValue, duration});
            }

            return null;
        }
    }
}