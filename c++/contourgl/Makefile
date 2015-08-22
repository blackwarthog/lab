DEPLIBS = gl x11

CXXFLAGS = -O3 -Wall -fmessage-length=0 `pkg-config --libs $(DEPLIBS)` -DGL_GLEXT_PROTOTYPES
DEPS = contour.h   contourbuilder.h   geometry.h   polyspan.h   rendersw.h   shaders.h   test.h \
       contour.cpp contourbuilder.cpp              polyspan.cpp rendersw.cpp shaders.cpp test.cpp
OBJS = contour.o contourgl.o contourbuilder.o polyspan.o rendersw.o shaders.o test.o
LIBS = `pkg-config --libs $(DEPLIBS)`
TARGET = contourgl

$(TARGET):	$(OBJS)
	$(CXX) -o $(TARGET) $(OBJS) $(LIBS)

all:	$(TARGET)

clean:
	rm -f $(OBJS) $(TARGET)
