using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace PracticeILAndDynamicMethods
{
    class Program
    {
        static int Calculate(int first, int second, int third)
        {
            var result = first * second;
            return result - third;
        }
        static int Divider(int first, int second)
        {
            return first / second;
        }
        static void Main(string[] args)
        {
            Console.WriteLine(ILDivider(10, 2).ToString());
            Console.WriteLine(ILCalculate(1, 2, 3).ToString());
            Console.WriteLine(Loop(2).ToString());
            CallMethods();
            Console.ReadLine();
        }

        static void CallMethods()
        {
            var myMethod = new DynamicMethod("MyMethod", typeof(void), null, typeof(Program).Module);
            var il = myMethod.GetILGenerator();
            il.Emit(OpCodes.Ldc_I4, 42);
            //when you put a value on the stack and then call a method, those value automatically become the parameteres of the method
            il.Emit(OpCodes.Call, typeof(Program).GetMethod("Print"));
            il.Emit(OpCodes.Ret);

            var method = (Action)myMethod.CreateDelegate(typeof(Action));
            method();

        }
        public static void Print(int i)
        {
            Console.WriteLine("The Value passed tot PRint is {0}", i);
        }

        delegate int DivideDelegate(int a, int b);
        static int Loop(int x)
        {
            //var result = 0;
            //for (int i = 0; i < 10; i++)
            //{
            //    result += i * x;
            //}
            //return result;

            //the code of above in IL
            var loop = new DynamicMethod("Loop", typeof(int), new[] { typeof(int) }, typeof(Program).Module);

            var il = loop.GetILGenerator();
            //this is just what you do when you are going to have "GOTO" statements and need a label to tell it where to "GOTO"
            var loopStart = il.DefineLabel();
            var methodEnd = il.DefineLabel();

            //we have a local variable called result, in IL you don't name variables, they are just indexes in the local variable stack
            il.DeclareLocal(typeof(int));
            //you have to put the value on the stack first, you can't just put it directly in the local variable, this is the direct constant of 0 
            il.Emit(OpCodes.Ldc_I4_0);
            //now store the 0 in the local variable of index 0
            il.Emit(OpCodes.Stloc_0);

            //now make the variable for the "i" of the loop
            //you can thing of this as adding another storage location to an array, there was 1 spot, now there are 2 spots
            il.DeclareLocal(typeof(int));
            //again you can't just put things in local variables you have to put them on the stack first
            il.Emit(OpCodes.Ldc_I4_0);
            //put 0 in the index of 1 as being the second variable
            il.Emit(OpCodes.Stloc_1);

            //we are starting the for loop so we need to mark where it starts in order to go back to it, this is just how you do this
            il.MarkLabel(loopStart);

            //now we are checking whether i is greater or equal to 10 in order to return out
            il.Emit(OpCodes.Ldloc_1);
            //there is no constant for 9 or above so this is how you load every other number
            il.Emit(OpCodes.Ldc_I4, 10);
            il.Emit(OpCodes.Bge, methodEnd);

            //now do the first operation in the loop. i * x
            //load the variable i from the local stack to the evaluation stack
            il.Emit(OpCodes.Ldloc_1);
            //load the method parameter x on the stack
            il.Emit(OpCodes.Ldarg_0);
            //multiply the two values and remove them from stack and add the result to the stack
            il.Emit(OpCodes.Mul);

            //now add the value to the variable result and store back into result
            //load the result variable onto the stack
            il.Emit(OpCodes.Ldloc_0);
            //now the value from result and the value from the multiplication are next to each other, do an Add
            il.Emit(OpCodes.Add);
            //now store the value back into the result variable
            il.Emit(OpCodes.Stloc_0);

            //now the loop is done and we need to increment the variable i
            //put i on the stack
            il.Emit(OpCodes.Ldloc_1);
            //put the number 1 on the stack
            il.Emit(OpCodes.Ldc_I4_1);
            //add them together which removes them from stack and put new result on stack
            il.Emit(OpCodes.Add);
            //store result back into i
            il.Emit(OpCodes.Stloc_1);

            //go back to beginnings of the loop
            il.Emit(OpCodes.Br, loopStart);

            il.MarkLabel(methodEnd);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ret);

            var method = (Func<int, int>)loop.CreateDelegate(typeof(Func<int, int>));
            return method(x);

        }
        static int ILDivider(int one, int two)
        {
            var divMethod = new DynamicMethod("DivideMethod", //name of method
                 typeof(int), //return type
                 new[] { typeof(int), typeof(int)},//parameters
                 typeof(Program).Module); //the module it lives in, remember these things called modules exist, even though you cannot make them in Visual Studio so nobody knows about them
            var il = divMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);//take the first argument to the method and loads it to the top of the evaluation stack
            il.Emit(OpCodes.Ldarg_1);//load second argument to the top of eval stack
            il.Emit(OpCodes.Div);// this will remove the last 2 values on the stack and divide them and then put the result on top of the stack
            il.Emit(OpCodes.Ret);


            //first way to call dynamic method
            var result = divMethod.Invoke(
                //we are not invoking the method in the context of an instance of an object so it can be null, therefore you could not use "this" in the method
                null,
                //an array of the arguments you are sending into the method
                new object[] { one, two }); 

            //second way to call dynamic method
            var method = (DivideDelegate)divMethod.CreateDelegate(typeof(DivideDelegate));

            result = method(one, two);

            return (int) result;
        
        }
        static int ILCalculate(int one, int two, int three)
        {
            //this is the IL version of the method Program.Calculate
            var calcMethod = new DynamicMethod("CalcMethod", //name of method
                typeof(int), //return type
                new[] { typeof(int), typeof(int), typeof(int) },//parameters
                typeof(Program).Module); //the module it lives in, remember these things called modules exist, even though you cannot make them in Visual Studio so nobody knows about them

            var il = calcMethod.GetILGenerator();
            //if you are going to store a local variable at any point in execution like Ldloc_x, then you have to call this to setup a place for the local variable
            il.DeclareLocal(typeof(int));
            il.Emit(OpCodes.Ldarg_0); // loads the first argument of the method into the evaluation stack, THE STACK
            il.Emit(OpCodes.Ldarg_1); // load second argument onto stack
            il.Emit(OpCodes.Mul); //take the 2 latest values that are on the stack (removing them) multiply them and put the result back on the stack
            //now the result of parameter 1 * paramter 2 is sitting on the stack
            //put the value on the stack into a local variable
            il.Emit(OpCodes.Stloc_0); //this takes the last value on the stack and removes it and puts it into local storage, not sure how you know to use 0 index at this point?
            //now take the value stored in local variable and put it back on the stack, yes this is weird but it is just what you do
            il.Emit(OpCodes.Ldloc_0); //this loads the local variable from location zero and pushes it onto the stack, it does not remove it from local variable
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Ret);

            var method = (Func<int, int, int, int>)calcMethod.CreateDelegate(typeof(Func<int, int, int, int>));
            return method(one, two, three);
        }
    }
}
