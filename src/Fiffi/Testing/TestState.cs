using System;
using System.Collections.Generic;
using System.Text;

namespace Fiffi.Testing
{
    public class TestState
    {
        public TestState When(IEvent @event) => this;
    }
}
