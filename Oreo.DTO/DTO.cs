using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace Oreo.DTO
{

    internal delegate object GetDTO();

    public class DTO
    {
        // Collection of deligates that return instances of our dynamic DTO's...
        private static readonly ConcurrentDictionary<Type, GetDTO> _instanceDelegates = new ConcurrentDictionary<Type, GetDTO>();

        /// <summary>
        /// Returns an instance of a Type that implements the given Interface.
        /// </summary>
        /// <typeparam name="TInterfaceType"></typeparam>
        /// <returns></returns>
        public static TInterfaceType Create<TInterfaceType>()
        {
            Type dtoInterfaceType = typeof(TInterfaceType);

            if (!dtoInterfaceType.IsInterface)
                throw new InvalidOperationException("Type must be an Interface.");

            // Have we already created the Type?
            if (!_instanceDelegates.ContainsKey(typeof(TInterfaceType)))
            {
                // No. Lets generate the Type...
                //
                // 1. Assembly Builder...
                AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);

                // 2. Module Builder...
                ModuleBuilder modBuilder = asmBuilder.DefineDynamicModule(dtoInterfaceType.FullName + ".dll");

                Type dtoType = CreateType(modBuilder, dtoInterfaceType);

                // Generate the Instancer method/delegate...
                ConstructorInfo dtoCtor = dtoType.GetConstructors(BindingFlags.Instance | BindingFlags.Public)[0];
                DynamicMethod dm = new DynamicMethod("GetDTO", dtoType, new Type[] { });
                ILGenerator il = dm.GetILGenerator();

                // Example: 
                //       Return New CustomerDTO()
                il.Emit(OpCodes.Newobj, dtoCtor);
                il.Emit(OpCodes.Ret);

                _instanceDelegates.TryAdd(dtoInterfaceType, (GetDTO)dm.CreateDelegate(typeof(GetDTO)));

            }

            return (TInterfaceType)_instanceDelegates[dtoInterfaceType].Invoke();

        }

        private static Type CreateType(ModuleBuilder modBuilder, Type dtoInterfaceType)
        {
            // 3. Type Builder...
            string dtoTypeName = dtoInterfaceType.Name + "DTO";
            TypeBuilder typeBilder = modBuilder.DefineType(dtoTypeName, TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed, typeof(object));

            // Define the constructor...
            ConstructorInfo baseCtor = typeof(object).GetConstructor(Type.EmptyTypes);
            ConstructorBuilder ctor = typeBilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
            ILGenerator ctorIl = ctor.GetILGenerator();

            typeBilder.AddInterfaceImplementation(dtoInterfaceType);

            foreach (PropertyInfo prop in dtoInterfaceType.GetRuntimeProperties())
            {
                FieldInfo backingField = BuildProperty(typeBilder, prop.Name, prop.PropertyType);

                Type nestedType = null;

                // Create DTO for nested reference types...
                if (modBuilder.Assembly.GetType(prop.PropertyType.Name + "DTO") == null && !prop.PropertyType.IsValueType && prop.PropertyType.IsInterface && !_instanceDelegates.ContainsKey(prop.PropertyType))
                {
                    nestedType = CreateType(modBuilder, prop.PropertyType);
                }

                if (nestedType != null)
                {
                    // Need to assign an instance...
                    ctorIl.Emit(OpCodes.Ldarg_0); // load 'this' onto the stack
                    ctorIl.Emit(OpCodes.Newobj, nestedType.GetConstructor(Type.EmptyTypes)); // invoke .ctor for new instance
                    ctorIl.Emit(OpCodes.Stfld, backingField); // assign the new instance to the backing field
                }
            }

            //
            ctorIl.Emit(OpCodes.Ldarg_0); // load 'this' onto the stack
            ctorIl.Emit(OpCodes.Call, baseCtor); // call 'base.New(this);'
            ctorIl.Emit(OpCodes.Ret); // return;

            // Bake it!
            Type dtoType = typeBilder.CreateType();

            return dtoType;
        }
        private static FieldInfo BuildProperty(TypeBuilder typeBuilder, string name, Type type)
        {
            FieldBuilder field = typeBuilder.DefineField("_" + name, type, FieldAttributes.Private);

            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(name, PropertyAttributes.None, type, null);

            MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual;

            MethodBuilder getter = typeBuilder.DefineMethod("get_" + name, getSetAttr, type, Type.EmptyTypes);

            ILGenerator getIL = getter.GetILGenerator();
            getIL.Emit(OpCodes.Ldarg_0);
            getIL.Emit(OpCodes.Ldfld, field);
            getIL.Emit(OpCodes.Ret);

            MethodBuilder setter = typeBuilder.DefineMethod("set_" + name, getSetAttr, null, new Type[] { type });

            ILGenerator setIL = setter.GetILGenerator();
            setIL.Emit(OpCodes.Ldarg_0);
            setIL.Emit(OpCodes.Ldarg_1);
            setIL.Emit(OpCodes.Stfld, field);
            setIL.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getter);
            propertyBuilder.SetSetMethod(setter);

            return field;
        }
    }
}
