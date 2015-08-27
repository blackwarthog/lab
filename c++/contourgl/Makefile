DEPLIBS = gl x11 OpenCL

CXXFLAGS = -O0 -g -Wall -fmessage-length=0 `pkg-config --cflags $(DEPLIBS)` -DGL_GLEXT_PROTOTYPES
DEPS = clcontext.h   contour.h   contourgl.h   contourbuilder.h   geometry.h   polyspan.h   rendersw.h   shaders.h   test.h   triangulator.h   \
       clcontext.cpp contour.cpp contourgl.cpp contourbuilder.cpp              polyspan.cpp rendersw.cpp shaders.cpp test.cpp triangulator.cpp
OBJS = clcontext.o   contour.o   contourgl.o   contourbuilder.o                polyspan.o   rendersw.o   shaders.o   test.o   triangulator.o
LIBS = `pkg-config --libs $(DEPLIBS)`
TARGET = contourgl

$(TARGET):	$(OBJS)
	$(CXX) -o $(TARGET) $(OBJS) $(LIBS)

all:	$(TARGET)

clean:
	rm -f $(OBJS) $(TARGET)
