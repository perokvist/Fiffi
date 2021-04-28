using System;

namespace Fiffi
{
    public abstract record Maybe<T>
    {
        public static explicit operator Maybe<T>(T value) =>
            Some(value);

        public static Maybe<T> Some(T value) =>
            new Choices.Some(value);

        public static Maybe<T> None { get; } = new Choices.None();

        public abstract R Match<R>(Func<T, R> someFunc, Func<R> noneFunc);

        public abstract void Iter(Action<T> someAction, Action noneAction);

        public Maybe<R> Map<R>(Func<T, R> map) =>
            Match(
                v => Maybe<R>.Some(map(v)),
                () => Maybe<R>.None);

        public R Fold<R>(Func<R, T, R> foldFunc, R seed) =>
            Match(t => foldFunc(seed, t), () => seed);

        public R GetOrElse<R>(Func<T, R> foldFunc, R seed) =>
            Fold((_, t) => foldFunc(t), seed);

        public T GetOrDefault(T defaultValue) =>
            Fold((_, t) => t, defaultValue);

        public static Maybe<T> Return(T value) =>
            Some(value);

        public Maybe<R> Bind<R>(Func<T, Maybe<R>> map) =>
            Match(
                v => map(v).Match(
                    r => Maybe<R>.Some(r),
                    () => Maybe<R>.None),
                () => Maybe<R>.None);

        private Maybe() { }

        private static class Choices
        {
            public record Some : Maybe<T>
            {
                private T Value { get; }

                public Some(T value) =>
                    Value = value;

                public override R Match<R>(Func<T, R> someFunc, Func<R> noneFunc) =>
                    someFunc(Value);

                public override void Iter(Action<T> someAction, Action noneAction) =>
                    someAction(Value);

                public override string ToString() =>
                    $"Some ({Value})";
            }

            public record None : Maybe<T>
            {
                public override R Match<R>(Func<T, R> someFunc, Func<R> noneFunc) =>
                    noneFunc();

                public override void Iter(Action<T> someAction, Action noneAction) =>
                    noneAction();

                public override string ToString() =>
                    "None";
            }
        }
    }
}
