using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SCMPG
{
    public static class ProbeFunctions
    {
        //obj.GetPrivateField<int>("privatefield1")
        public static T GetPrivateField<T>(this object instance, string fieldname)
        {
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = instance.GetType();
            FieldInfo field = type.GetField(fieldname, flag);
            return (T)field.GetValue(instance);
        }
        //obj.GetPrivateProperty<string>("PrivateFieldA")
        public static T GetPrivateProperty<T>(this object instance, string propertyname)
        {
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = instance.GetType();
            PropertyInfo field = type.GetProperty(propertyname, flag);
            return (T)field.GetValue(instance, null);
        }
        // ProbeFunctions.SetPrivateField(item.ComponentFlu, "m_fluDuration", 900);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="instance">Instance</param>
        /// <param name="fieldname">FieldName</param>
        /// <param name="value">Value</param>
        public static void SetPrivateField(this object instance, string fieldname, object value)
        {
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = instance.GetType();
            FieldInfo field = type.GetField(fieldname, flag);
            field.SetValue(instance, value);
        }
        //obj.SetPrivateProperty("PrivateFieldA", "hello");
        public static void SetPrivateProperty(this object instance, string propertyname, object value)
        {
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = instance.GetType();
            PropertyInfo field = type.GetProperty(propertyname, flag);
            field.SetValue(instance, value, null);
        }
        //obj.CallPrivateMethod<int>("Add",null)
        public static T CallPrivateMethod<T>(this object instance, string name, params object[] param)
        {
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = instance.GetType();
            MethodInfo method = type.GetMethod(name, flag);
            return (T)method.Invoke(instance, param);
        }
    }
}
/*
 // 我们所需要的反射获取的类中的方法结构如下
private class A()
{

    private static class B(){
    private void func()
    {
        // run ...
    }
}
private static B b;
}
 
public void MyReflect()
{
    // 反射获取静态类B
    myNestedType = typeof(A).GetNestedType("B", BindingFlags.NonPublic | BindingFlags.Instance)

    // B的静态实例
    fieldInfo = typeof(A).GetField("b", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

    // 获取静态类B中的函数func
    method = myNestedType.GetMethod("func", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

    object[] invokeArgs = new object[];
    // 调用反射获取的函数func
    // 因为是b是静态的，所以filedInfo.GetValue中传入的参数是null
    method.Invoke(filedInfo.GetValue(null), null)
}
*/