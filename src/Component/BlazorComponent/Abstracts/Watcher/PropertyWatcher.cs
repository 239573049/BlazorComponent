﻿using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace BlazorComponent
{
    /// <summary>
    /// This may has bugs,do not use now
    /// </summary>
    public sealed class PropertyWatcher
    {
        private readonly ConcurrentDictionary<string, IObservableProperty> _props = new();
        private readonly Type _objectType;

        public PropertyWatcher(Type objectType)
        {
            _objectType = objectType;
        }

        public TValue? GetValue<TValue>(TValue? @default = default, string name = "", bool disableIListAlwaysNotifying = false)
        {
            var property = GetProperty(@default, name, disableIListAlwaysNotifying);

            if (!property.HasValue)
            {
                property.SetValueWithNoEffect(@default);
            }

            return property.Value;
        }

        private ObservableProperty<TValue> GetProperty<TValue>(TValue? @default, string name, bool disableIListAlwaysNotifying = false)
        {
            var prop = _props.GetOrAdd(name, _ => new ObservableProperty<TValue>(name, @default, disableIListAlwaysNotifying));
            if (prop.GetType() == typeof(ObservableProperty))
            {
                //Internal watch may before `ObservableProperty<TValue>` be created 
                prop = new ObservableProperty<TValue>(prop, @default, disableIListAlwaysNotifying);
                _props[name] = prop;
            }

            var property = (ObservableProperty<TValue>)prop;

            property.DisableIListAlwaysNotifying = disableIListAlwaysNotifying;

            return property;
        }

        public TValue? GetComputedValue<TValue>(string name)
        {
            var property = GetProperty<TValue>(default, name);
            return !property.HasValue ? default : property.Value;
        }

        public TValue? GetComputedValue<TValue>(Expression<Func<TValue>> valueExpression, string name)
        {
            var property = GetProperty<TValue>(default, name);
            if (!property.HasValue)
            {
                var valueFactory = valueExpression.Compile();
                property.ValueFactory = valueFactory;
                property.Value = valueFactory();

                //Analysis the dependency property and watch them,so when they have changes,we will re-compute the value
                var visitor = new MemberAccessVisitor();
                visitor.Visit(valueExpression);

                var propertyInfos = visitor.PropertyInfos.Where(r => r.DeclaringType is not null && _objectType.IsSubclassOf(r.DeclaringType));
                foreach (var propertyInfo in propertyInfos)
                {
                    Watch(propertyInfo.Name, () =>
                    {
                        var value = valueFactory();
                        SetValue(value, name);
                    });
                }
            }

            return property.Value;
        }

        public TValue? GetComputedValue<TValue>(Func<TValue> valueFactory, string[] dependencyProperties, string name)
        {
            var property = GetProperty<TValue>(default, name);
            if (!property.HasValue)
            {
                property.ValueFactory = valueFactory;
                property.Value = valueFactory();

                foreach (var dependencyProperty in dependencyProperties)
                {
                    Watch(dependencyProperty, () =>
                    {
                        var value = valueFactory();
                        SetValue(value, name);
                    });
                }
            }

            return property.Value;
        }

        public void SetValue<TValue>(TValue value, string name, bool disableIListAlwaysNotifying = false)
        {
            var property = GetProperty<TValue>(default, name, disableIListAlwaysNotifying);
            property.Value = value;
        }

        /// <summary>
        /// setting value can only be executed after a given property has been set first.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="name"></param>
        /// <param name="propertySetFirst"></param>
        /// <typeparam name="TValue"></typeparam>
        /// <typeparam name="TFirstValue"></typeparam>
        public void SetValue<TValue, TFirstValue>(TValue value, string name, string propertySetFirst)
        {
            var property = GetProperty<TFirstValue>(default, propertySetFirst);
            if (property.HasValue)
            {
                Unwatch(propertySetFirst);
                SetValue(value, name);
            }
            else
            {
                Watch(propertySetFirst, () => { SetValue(value, name); });
            }
        }

        public PropertyWatcher Watch<TValue>(string name, Action changeCallback, bool immediate = false, bool @override = false,
            TValue? defaultValue = default)
        {
            return Watch(name, (_, _) => changeCallback.Invoke(), immediate, @override, defaultValue);
        }

        public PropertyWatcher Watch<TValue>(string name, Action<TValue?> changeCallback, bool immediate = false, bool @override = false,
            TValue? defaultValue = default)
        {
            return Watch(name, (newValue, _) => changeCallback.Invoke(newValue), immediate, @override, defaultValue);
        }

        public PropertyWatcher Watch<TValue>(string name, Action<TValue?, TValue?> changeCallback, bool immediate = false, bool @override = false,
            TValue? defaultValue = default)
        {
            if (@override)
            {
                Unwatch(name);
            }

            var property = GetProperty<TValue>(default, name);
            property.OnValueChange += changeCallback;

            // TODO: defaultValue should not set to property, also in GetValue(defaultValue)
            if (defaultValue is not null && !EqualityComparer<TValue>.Default.Equals(defaultValue, default))
            {
                property.Value = defaultValue;
            }

            if (immediate)
            {
                changeCallback.Invoke(property.Value, default);
            }

            return this;
        }

        public PropertyWatcher Watch<TValue>(string name, Action<TValue?, TValue?> changeCallback, Func<TValue> valueFactory,
            string[] dependencyProperties, bool immediate = false, bool @override = false)
        {
            if (@override)
            {
                Unwatch(name);
            }

            var property = GetProperty<TValue>(default, name);
            property.OnValueChange += changeCallback;
            property.ValueFactory = valueFactory;
            property.Value = valueFactory();

            foreach (var dependencyProperty in dependencyProperties)
            {
                Watch(dependencyProperty, () =>
                {
                    var value = valueFactory();
                    SetValue(value, name);
                });
            }

            if (immediate)
            {
                changeCallback.Invoke(property.Value, default);
            }

            return this;
        }

        private void Unwatch(string name)
        {
            _props.TryRemove(name, out _);
        }

        private void Watch(string name, Action changeCallback)
        {
            //Internal watch can not infer the TValue,can we get a better solution?
            var prop = _props.GetOrAdd(name, _ => new ObservableProperty(name));
            prop.OnChange += _ => changeCallback.Invoke();
        }
    }
}
