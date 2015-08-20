DEPLIBS = gl x11

CXXFLAGS = -O3 -Wall -fmessage-length=0 `pkg-config --libs $(DEPLIBS)` -DGL_GLEXT_PROTOTYPES
DEPS = contour.h contourbuilder.h
OBJS = contour.o contourgl.o contourbuilder.o
LIBS = `pkg-config --libs $(DEPLIBS)`
TARGET = contourgl

$(TARGET):	$(OBJS)
	$(CXX) -o $(TARGET) $(OBJS) $(LIBS)

all:	$(TARGET)

clean:
	rm -f $(OBJS) $(TARGET)
