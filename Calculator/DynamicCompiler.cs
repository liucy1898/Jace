﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Calculator.Operations;

namespace Calculator
{
    public class DynamicCompiler : IExecutor
    {
        public double Execute(Operation operation)
        {
            return Execute(operation, new Dictionary<string, double>());
        }

        public double Execute(Operation operation, Dictionary<string, int> variables)
        {
            Dictionary<string, double> doubleVariables = new Dictionary<string, double>();
            foreach (string key in variables.Keys)
                doubleVariables.Add(key, variables[key]);

            return Execute(operation, doubleVariables);
        }

        public double Execute(Operation operation, Dictionary<string, double> variables)
        {
            DynamicMethod method = new DynamicMethod("MyCalcMethod", typeof(double),
                new Type[] { typeof(Dictionary<string, double>) });
            ILGenerator generator = method.GetILGenerator();
            generator.DeclareLocal(typeof(double));
            GenerateMethodBody(generator, operation, variables);
            generator.Emit(OpCodes.Ret);

            Func<Dictionary<string, double>, double> function = 
                (Func<Dictionary<string, double>, double>)method.CreateDelegate(typeof(Func<Dictionary<string, double>, double>));
            return function(variables);
        }

        private void GenerateMethodBody(ILGenerator generator, Operation operation, Dictionary<string, double> variables)
        {
            if (operation == null)
                throw new ArgumentNullException("operation");

            if (operation.GetType() == typeof(IntegerConstant))
            {
                IntegerConstant constant = (IntegerConstant)operation;
                
                generator.Emit(OpCodes.Ldc_I4, constant.Value);
                generator.Emit(OpCodes.Conv_R8);
            }
            else if (operation.GetType() == typeof(FloatingPointConstant))
            {
                FloatingPointConstant constant = (FloatingPointConstant)operation;

                generator.Emit(OpCodes.Ldc_R8, constant.Value);
            }
            else if (operation.GetType() == typeof(Variable))
            {
                Type dictionaryType = typeof(Dictionary<string, double>);

                Variable variable = (Variable)operation;
                //if (variables.ContainsKey(variable.Name))
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldstr, variable.Name);
                generator.Emit(OpCodes.Callvirt, dictionaryType.GetMethod("Add", new Type[] { typeof(string), typeof(double) }));
                //else
                //    throw new VariableNotDefinedException(string.Format("The variable \"{0}\" used is not defined.", variable.Name));
            }
            else if (operation.GetType() == typeof(Multiplication))
            {
                Multiplication multiplication = (Multiplication)operation;
                GenerateMethodBody(generator, multiplication.Argument1, variables);
                GenerateMethodBody(generator, multiplication.Argument2, variables);

                generator.Emit(OpCodes.Mul);
            }
            else if (operation.GetType() == typeof(Addition))
            {
                Addition addition = (Addition)operation;
                GenerateMethodBody(generator, addition.Argument1, variables);
                GenerateMethodBody(generator, addition.Argument2, variables);

                generator.Emit(OpCodes.Add);
            }
            else if (operation.GetType() == typeof(Substraction))
            {
                Substraction addition = (Substraction)operation;
                GenerateMethodBody(generator, addition.Argument1, variables);
                GenerateMethodBody(generator, addition.Argument2, variables);

                generator.Emit(OpCodes.Sub);
            }
            else if (operation.GetType() == typeof(Division))
            {
                Division division = (Division)operation;
                GenerateMethodBody(generator, division.Dividend, variables);
                GenerateMethodBody(generator, division.Divisor, variables);

                generator.Emit(OpCodes.Div);
            }
            else
            {
                throw new ArgumentException(string.Format("Unsupported operation \"{0}\".", operation.GetType().FullName), "operation");
            }
        }
    }
}
