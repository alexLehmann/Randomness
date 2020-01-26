using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Randomness
{
    public class FromDistribution : Attribute
    {
        public Type Distribution { get; set; }
        public double[] Parameters { get; set; }
        public FromDistribution(Type distribution, params double[] ps)
        {
            this.Distribution = distribution;
            this.Parameters = ps;
        }
    }
           
   
    public  class SubGenerator<TParameter> 
         where TParameter : new()
    {
        private PropertyInfo SetProperty { get; set;}
        private Generator<TParameter> Generator { get; set; }
        public SubGenerator(Generator<TParameter> generator, PropertyInfo setProperty) 
        {
            Generator = generator;
            SetProperty = setProperty;
        } 
        
        public Generator<TParameter> Set(IContinousDistribution continousDistribution)
        {
            if (Generator<TParameter>.dictionaryDistribution.ContainsKey(SetProperty))
                Generator<TParameter>.dictionaryDistribution[SetProperty] = continousDistribution;
            else throw new ArgumentException();
            return Generator;
        }
    }
   
    public class Generator<TParameter>
        where TParameter : new()
    {
        public static Dictionary<PropertyInfo, IContinousDistribution> dictionaryDistribution;
        private PropertyInfo[] properties;
        private FromDistribution[] parameters;       

        public Generator()
        {
            dictionaryDistribution = new Dictionary<PropertyInfo, IContinousDistribution>();
            properties = typeof(TParameter)
                 .GetProperties()                 
                 .ToArray();

            parameters = typeof(TParameter)
                .GetProperties()
                .Select(z => z.GetCustomAttributes(typeof(FromDistribution), false))
                .Where(z => z.Length != 0)
                .Select(z => z[0])
                .Cast<FromDistribution>()
                .ToArray();

            for (int i = 0, j = 0; i < properties.Length; i++)
            {
                if (properties[i].CustomAttributes.Count() != 0)
                {
                    var constructor = parameters[j].Distribution
                        .GetConstructors()
                        .Where(z => z.GetParameters().Length == parameters[j].Parameters.Length)
                        .FirstOrDefault();
                    if (constructor is null)
                        throw new ArgumentException(parameters[j].Distribution.Name);
                    var distributionObject = constructor.Invoke(parameters[j].Parameters.Cast<object>().ToArray());
                    dictionaryDistribution[properties[i]] = (IContinousDistribution)distributionObject;
                    j++;
                }
                else dictionaryDistribution[properties[i]] = null;
            }            
        }

        public TParameter Generate(Random rand)
        {
            TParameter obj = new TParameter();
            foreach (var item in dictionaryDistribution)
            {
                if(item.Value != null)
                    item.Key.SetValue(obj, item.Value.Generate(rand));
            }            
            return obj;
        }       

        public SubGenerator<TParameter> For(Expression<Func<TParameter, double>> function)
        {           
            var bodyDelegate = function.Body;
            if (!(bodyDelegate is MemberExpression))
                throw new ArgumentException();            
            var memberDelegate = (MemberExpression)bodyDelegate;
            var propertyDelegete = memberDelegate.Member;
            var property = (PropertyInfo)propertyDelegete;
            if (!dictionaryDistribution.ContainsKey(property))
                throw new ArgumentException(property.Name);
            return new SubGenerator<TParameter>(this,property);
        }

    }    
}
