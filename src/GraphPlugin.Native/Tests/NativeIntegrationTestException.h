#pragma once

using namespace System;

namespace GraphPlugin::Native::Tests
{
public
ref class NativeIntegrationTestException sealed : System::Exception
{
  public:
    NativeIntegrationTestException(String ^ message) : Exception(message) {}
};
} // namespace GraphPlugin::Native::Tests