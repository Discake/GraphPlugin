#pragma once

using namespace HostMgd::ApplicationServices;

namespace GraphPlugin::Native::Tests
{
    public ref class StyleInteropScenario abstract sealed
    {
    public:
        static void Prepare(Document^ document);

        static void Execute(Document^ document);
    };
}
