---
layout: post
title: Faster Python Protobuf
published: 2020-01-04
---

It's no secret and no surprise that Protobuf in Python is slower than its Java and C++ counterparts, [here's the official benchmarks](https://github.com/protocolbuffers/protobuf/blob/02a4c720c31af4e028469362b7e43b04ab2d2d4e/docs/performance.md). However if you look at those benchmarks there is what appears to be a secret, at least as far as the documentation goes, in the benchmarks it goes by the column header: "C++-generated-code".

<!--more-->

The benchmarks list three variants for Python: "C++-generated-code", "C++-reflection", and "pure-Python". A little bit about these:
* Pure Python is the Protobuf library implemented in Python alone, understandably not performant. 
* C++ reflection is the Protobuf library but makes use of a Python extension which links to the C++ Protobuf library (`libprotobuf.so`), this gives a significant boost to performance over the Python library, orders of magnitude faster than the pure Python variant (roughly 10x). 
* C++ generated code links to C++ libraries which have been generated from the proto, for serialisation this is orders of magnitude faster again (roughly 5x for serialisation and 2x for parsing).

## So which of these variants do we get when we install protobuf via pip?

If your pip is hitting PyPi (default) you're going to be picking up one of the [wheels packages](https://pypi.org/project/protobuf/#files), each of the wheels that are specific about their platform make use of the "C++-reflection" variant - you're likely to be picking up one of these. However of course not every platform is covered, for this the "pure-Python" variant is used as a fallback, so you need to be careful that you're not going to suddenly get undetected performance regressions which would [happen today if you updated Python 3.7 to Python 3.8 on Windows for example](https://github.com/protocolbuffers/protobuf/issues/6214). You can detect if you're using "C++-reflection" or "pure-Python" using:

```bash
python -c "from google.protobuf.internal import api_implementation; print(api_implementation._default_implementation_type)"
```

The above will print either `cpp` or `python`, note that it's using an internal api so there's no guarantee this won't break in future versions of the Protobuf library.

## So what about the faster "C++-generated-code" variant?

Disclaimer: This variant is a bit more involved, unless you know need the additional performance I wouldn't recommend it.

To make use of generated C++ libraries from your protos from the Python library requires magic. The gist of it is that the C++ Protobuf library contains what's named a `DescriptorPool`, this is global and contains descriptions of all loaded Protobuf messages. The generated C++ code hooks into this by [adding the description of the message to the pool](https://github.com/protocolbuffers/protobuf/blob/5c028d6cf42e426d47f5baa6ea3f0f5c86b97beb/src/google/protobuf/any.pb.cc#L81). The Python library that links to the C++ library via a Python extension can make use of the global description pool from the C++ library. *Note description this may not be 100% accurate and there's more involved, but this is the gist of how it's functioning.*

So, what we need is to load the C++ libraries generated from your `.proto` files before importing your generated Python code. This will get the C++ code for the messages added into the global descriptor pool which the Python library is then able to use. Which leads to the question, how do we load the C++ libraries? A. A Python extension that links to the C++ Protobuf library and to the generated C++ library.

There's a gotcha with this. For this to work both your extension and the Protobuf library need to be making use of the same globals - they need to be linked to the same `libprotobuf` library. But the wheels packages on PyPi do not dynamically link, they appear to have been statically linked to `libprotobuf`: [setup.py](https://github.com/protocolbuffers/protobuf/blob/26c0fbc15bd2ed8371df3f737951804f0a92db4b/python/setup.py#L185).

We need to build the Protobuf Library ourselves. There's a few ways to do this, for example cloning the Protobuf git repository and building from source. Or another method, install `libprotobuf` and grab the source from `PyPi` an, example:

```bash
# Install C++ Protobuf Library and headers
sudo apt-get install libprotobuf-dev

# Download source from PyPi (alternatively clone the git repo for latest)
python -m pip download --no-deps --no-binary=protobuf protobuf
mkdir protobuf
tar xzf protobuf-*.tar.gz -C protobuf --strip-components 1

# Install Python Protobuf Library with --cpp_implementation but importantly not --compile_static_extension
cd protobuf
python setup.py install --user --cpp_implementation
```

*Complete example + benchmarks coming soon*

*Note this was written at 2021-01-04, the situation may have changed since - all links are provided are the latest commits on the relevant files at the time of writing.*