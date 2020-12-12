using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pinboard.Entries;
using UnityEditor;
using UnityEngine;

namespace Pinboard
{
	public static class GenericControlUtility
	{
		public static Dictionary<string, GenericControlDefinition> GetAllDefinitions()
		{
			var definitions = new Dictionary<string, GenericControlDefinition>();
			var attrType = typeof(GenericControlAttribute);
			var boolType = typeof(bool);
			var voidType = typeof(void);
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (var type in assembly.GetTypes())
				{
					var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
					                  .Where(m => m.IsDefined(attrType)).ToList();
					if (methods.Count > 0)
						Debug.Log(methods.Count);
					var attrs = methods.Select(
						m => (m, attr: m.GetCustomAttribute(attrType) as GenericControlAttribute));
					var groups = attrs.GroupBy(tuple => tuple.attr.name);
					foreach (IGrouping<string, (MethodInfo, GenericControlAttribute)> group in groups)
					{
						var validator =
							group.FirstOrDefault(t => t.Item2.isValidateMethod && t.Item1.ReturnType == boolType &&
							                          t.Item1.GetParameters().Length == 0);
						if (validator == default)
						{
							Debug.LogWarning($"Generic control \"{group.Key}\" does not have a validation method.");
							continue;
						}

						var getter = group.FirstOrDefault(t => !t.Item2.isValidateMethod &&
						                                       t.Item1.ReturnType != voidType &&
						                                       t.Item1.GetParameters().Length == 0);
						if (getter == default)
						{
							Debug.LogWarning($"Generic control \"{group.Key}\" does not have a getter method.");
							continue;
						}
						

						var setter = group.FirstOrDefault(t => !t.Item2.isValidateMethod &&
						                                       t.Item1.ReturnType == voidType &&
						                                       t.Item1.GetParameters().Length == 1);
						if (setter == default)
						{
							Debug.LogWarning($"Generic control \"{group.Key}\" does not have a setter method.");
							continue;
						}

						if (getter.Item1.ReturnType != setter.Item1.GetParameters().First().ParameterType)
						{
							Debug.LogWarning(
								$"Generic control \"{group.Key}\" has incompatible getter and setter. Getter returns type {getter.Item1.ReturnType.FullName}, setter takes type {setter.Item1.GetParameters().First().ParameterType.FullName}");
							continue;
						}

						var definition = new GenericControlDefinition();
						definition.name = group.Key;
						definition.typeName = type.FullName;
						definition.validatorName = validator.Item1.Name;
						definition.getterName = getter.Item1.Name;
						definition.setterName = setter.Item1.Name;
						definitions.Add(group.Key, definition);
					}
				}
			}

			return definitions;
		}
	}
}
