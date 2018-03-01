using System;
using System.Collections.Generic;
using Yaw.Tests.CoreTest.Helpers;
using Yaw.Core;
using Yaw.Core.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Yaw.Tests.CoreTest
{
    /// <summary>
    ///This is a test class for TypeExtensionsTest and is intended
    ///to contain all TypeExtensionsTest Unit Tests
    ///</summary>
	[TestClass]
	public class TypeExtensionsTest
	{
    	/// <summary>
    	///Gets or sets the test context which provides
    	///information about and functionality for the current test run.
    	///</summary>
    	public TestContext TestContext { get; set; }

    	/// <summary>
		///A test for IsInheritedFromType
		///</summary>
		[TestMethod]
		public void IsInheritedFromTypeTest()
		{
			// проверяемый тип OtherTestSubsystem -> TestSubsystem -> Subsystem
			Type trueTestType = typeof(OtherTestSubsystem);
			// тип от которого унаследовались
			Type baseType = typeof(Subsystem);

			var actual = trueTestType.IsInheritedFromType(baseType);
			Assert.AreEqual(true, actual, "Проверяемый тип OtherTestSubsystem является наследником типа Subsystem");

			//тип не являющийся наследником Subsystem
			Type falseTestType = typeof(TestConfigurationSection);

			actual = falseTestType.IsInheritedFromType(baseType);
			Assert.AreEqual(false, actual, "Проверяемый тип TestConfigurationSection не является наследником типа Subsystem");
		}

		/// <summary>
		///A test for IsImplementInterface
		///</summary>
		[TestMethod]
		public void IsImplementInterfaceTest()
		{
			// проверяемый тип
			Type testType = typeof(StateSubsystem);
			// интерфейс реализованный типом StateSubsystem
			Type implementInterfaceType = typeof(IStateSubsystem);
			// интерфейс не реализованный типом StateSubsystem
			Type notImplementInterfaceType = typeof(ICoreApplication);
			
			// проверка значения true
			var actual = testType.IsImplementInterface(implementInterfaceType);
			Assert.AreEqual(true, actual, "Интерфейс IStateSubsystem реализован типом StateSubsystem");

			// проверка значения false
			actual = testType.IsImplementInterface(notImplementInterfaceType);
			Assert.AreEqual(false, actual, "Интерфейс ICoreApplication не реализован типом StateSubsystem");
		}

		/// <summary>
		///A test for GetProperty
		///</summary>
		[TestMethod]
		public void GetPropertyTest()
		{
			// проверяемый тип
			Type type = typeof(TestType);
			string propertyName = "PropertyWithGetSetAccessor";

			var actual = type.GetProperty(propertyName, true, true);

			Assert.AreEqual(propertyName, actual.Name, "Найдено неверное свойство");
		}

		/// <summary>
		///A test for GetProperty, получение свойства без Get аксессора
		///</summary>
		[TestMethod]
		public void GetPropertyWithOutGetAccessorTest()
		{
			// проверяемый тип
			Type type = typeof(TestType);
			string propertyName = "PropertyWithOutGetAccessor";

			try
			{
				type.GetProperty(propertyName, true, false);
				Assert.Fail("Не произошло исключения при получении свойства без get Accessor");
			}
			catch(Exception ex)
			{
				Assert.AreEqual("Свойства PropertyWithOutGetAccessor типа TestType не содержит public get аксессора",
									ex.Message, "Неверный текст исключения при попытке получить свойство без get Accessor");
			}
		}

		/// <summary>
		///A test for GetProperty, получение свойства без Set аксессора
		///</summary>
		[TestMethod]
		public void GetPropertyWithOutSetAccessorTest()
		{
			// проверяемый тип
			Type type = typeof(TestType);
			string propertyName = "PropertyWithOutSetAccessor";

			try
			{
				type.GetProperty(propertyName, false, true);
				Assert.Fail("Не произошло исключения при получении свойства без set Accessor");
			}
			catch (Exception ex)
			{
				Assert.AreEqual("Свойства PropertyWithOutSetAccessor типа TestType не содержит public set аксессора",
									ex.Message, "Неверный текст исключения при попытке получить свойство без set Accessor");
			}

		}

		/// <summary>
		///A test for GetProperty, получение несуществующего свойства
		///</summary>
		[TestMethod]
		public void GetNotExistedPropertyTest()
		{
			// проверяемый тип
			Type type = typeof(TestType);
			string propertyName = "NonExistedProperty";

			try
			{
				type.GetProperty(propertyName, false, false);
				Assert.Fail("Не произошло исключения при получении несуществующего свойства");
			}
			catch (Exception ex)
			{
				Assert.AreEqual("Тип TestType не содержит свойства public NonExistedProperty",
									ex.Message, "Неверный текст исключения при попытке получить несуществующее свойство");
			}
		}

		/// <summary>
		///A test for GetMethodParametersTypes
		///</summary>
		[TestMethod]
		public void GetMethodParametersTypesTest()
		{
			Type type = typeof(TestType);
			string methodName = "TestMethodForGetParametersType";
			var expected = new List<Type>();
			// типы параметров передваемого метода
			expected.AddRange(new[] {typeof(string), typeof(Int16), typeof(System.Diagnostics.TraceEventType)});
			
			var actual = type.GetMethodParametersTypes(methodName);
			// проверим, что все полученные значения содержатся в предполагаемых
			foreach (var actualType in actual)
			{
				Assert.IsTrue(expected.Contains(actualType),
				              String.Format("Полученный тип {0} не содержится среди параметров метода {1}",
				                            actualType.Name, methodName));
			}
			// проверим количество ожидаемых и полученных типов совпадают
			Assert.AreEqual(3,
			                actual.Length,
			                String.Format("Получено меньше параметров, чем содержит метод {0} типа TestTypes", methodName));
		}

		/// <summary>
		///A test for CanCastToType
		///</summary>
		[TestMethod]
		public void CanCastToTypeTest()
		{
			Type testType = typeof(TestType);
			Type castType1 = typeof(Subsystem);
			Type castType2 = typeof(object);

			// отрицательный результат
			var actual = testType.CanCastToType(castType1);
			Assert.AreEqual(false, actual, "Тип TestType нельзя привести к типу Subsystem");

			// положительный результат
			actual = testType.CanCastToType(castType2);
			Assert.AreEqual(true, actual, "Тип TestType можно привести к типу object");
		}

		/// <summary>
		///A test for GetMethodReturnType
		///</summary>
		[TestMethod]
		public void GetMethodReturnTypeTest()
		{
			Type type = typeof(TestType);
			string methodName = "TestMethodForGetParametersType";
			Type expected = typeof(void);

			var actual = type.GetMethodReturnType(methodName);
			Assert.AreEqual(expected, actual);
		}

		/// <summary>
		///A test for FindMethod, неверные параметры метода
		///</summary>
		[TestMethod]
		public void FindMethodWrongSignatureTest()
		{
			var methodDelegateType = typeof (Func<TestType, String, String>);
			string methodName = "TestMethod";
			var methodOwner = new TestType();
			try
			{
				methodDelegateType.FindMethod(methodName, methodOwner);
				Assert.Fail("Не возникло исключение при вызове метода с неверной сигнатурой");
			}
			catch (Exception exception)
			{
				Assert.AreEqual("Метод TestMethod не найден или сигнатура метода не соответствует требуемой",
				                exception.Message);
			}
		}

		/// <summary>
		///A test for FindMethod
		///</summary>
		[TestMethod]
		public void FindMethodTest()
		{
			var methodDelegateType = typeof(Func<String, String>);
			string methodName = "TestMethod";
			var methodOwner = new TestType();

			var actual = methodDelegateType.FindMethod(methodName, methodOwner);
			Assert.AreEqual(methodName, actual.Name, "Найден неверный метод");
			Assert.AreEqual(typeof(String), actual.ReturnParameter.ParameterType, "Найден неверный метод");
			Assert.AreEqual(1, actual.GetParameters().Length, "Найден неверный метод");
		}

		/// <summary>
		///A test for FindMethod
		///</summary>
		[TestMethod]
		[ExpectedException(typeof(ArgumentException), "Тип не является делегатом")]
		public void FindMethodNotDelegateTest()
		{
			Type methodDelegateType = typeof(String);

			object methodOwner = "Test String";
			methodDelegateType.FindMethod("ToString", methodOwner);
		}
	}
}
