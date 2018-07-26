# config

# just comment following line to disable CUDA
CUDA = cuda-9.2
CUDA_PATH := /opt/$(CUDA)
CUDA_BIN := $(CUDA_PATH)/bin
CUDA_PKGCONFIG := $(CUDA_PATH)/pkgconfig

DEPLIBS = gl x11 OpenCL


# compute build options

CXXFLAGS := $(CXXFLAGS) -O3 -Wall -fmessage-length=0 -DGL_GLEXT_PROTOTYPES
CXXFLAGS := $(CXXFLAGS) $(shell pkg-config --cflags $(DEPLIBS))
LIBS := $(LIBS) $(shell pkg-config --libs $(DEPLIBS))

ifdef CUDA
	CXXFLAGS := $(CXXFLAGS) -DCUDA $(shell PKG_CONFIG_PATH=$(CUDA_PKGCONFIG) pkg-config --cflags $(CUDA))
	LIBS := $(LIBS) $(shell PKG_CONFIG_PATH=$(CUDA_PKGCONFIG) pkg-config --libs $(CUDA))
endif


# files lists

TARGET = contourgl

SOURCES = \
	contourgl.cpp \
	clcontext.cpp \
	clrender.cpp \
	contour.cpp \
	contourbuilder.cpp \
	environment.cpp \
	geometry.cpp \
	glcontext.cpp \
	measure.cpp \
	polyspan.cpp \
	shaders.cpp \
	swrender.cpp \
	test.cpp \
	triangulator.cpp \
	utils.cpp

ifdef CUDA
	SOURCES += \
		cudacontext.cpp \
		cudarender.cpp
	CUDA_SOURCES = \
		cuda/contour.cu \
		cuda/hello.cu
endif


# files lists postprocessing 

OBJS = $(SOURCES:.cpp=.o)
DEPS = $(SOURCES:.cpp=.d)
PTXS = $(CUDA_SOURCES:.cu=.ptx)

# internal targets

%.ptx: %.cu
	$(CUDA_PATH)/bin/nvcc -ptx $< -o $@

# rule for make *.d files with include (.h) dependencies
%.d: %.cpp
	@set -e; rm -f $@; \
	 $(CXX) -MM $(CXXFLAGS) $< > $@.$$$$; \
	 sed 's,\($*\)\.o[ :]*,\1.o $@ : ,g' < $@.$$$$ > $@; \
	 rm -f $@.$$$$

$(TARGET): $(PTXS) $(DEPS) $(OBJS)
	$(CXX) -o $(TARGET) $(OBJS) $(LIBS)

# include rules with include (.h) dependencies
-include $(DEPS)

# actual targets

# declare that 'all', 'cuda' and 'clean' are cannot be a filenames 
.PHONY: all clean

all: $(TARGET)

clean:
	rm -f $(PTXS) $(OBJS) $(DEPS) $(TARGET)
