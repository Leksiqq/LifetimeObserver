**Attention!** _This article, as well as this announcement, are automatically translated from Russian_.


The **Net.Leksi.LifetimeObserver** library is designed to monitor the lifetime of objects of selected types. It is recommended to use the library to ensure that there are no memory leaks during development. Used with the standard dependency injection container `Microsoft.Extensions.DependencyInjection`.

All classes are contained in the `Net.Leksi.Util` namespace.

* [LifetimeObserver](https://github.com/Leksiqq/LifetimeObserver/wiki/LifetimeObserver-en) - library facade class. Registers in the dependency injection container as a _Singleton_ service.
* [LifetimeObserverException](https://github.com/Leksiqq/LifetimeObserver/wiki/LifetimeObserverException-en) - Specialized exception type.
* [LifetimeEventArgs](https://github.com/Leksiqq/LifetimeObserver/wiki/LifetimeEventArgs-en) - the type of the event argument associated with the object's lifetime.

The principles of using the library are described in the section [Demo and principles of use](https://github.com/Leksiqq/LifetimeObserver/wiki/Demo-and-principles-of-use).

Sources are [here](https://github.com/Leksiqq/LifetimeObserver/tree/master)

NuGet Package: [Net.Leksi.LifetimeObserver](https://www.nuget.org/packages/Net.Leksi.LifetimeObserver/)
