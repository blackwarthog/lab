DEPS = gl x11

CXXFLAGS = -O2 -g -Wall -fmessage-length=0 `pkg-config --libs $(DEPS)` -DGL_GLEXT_PROTOTYPES
OBJS = contourgl.o
LIBS = `pkg-config --libs $(DEPS)`
TARGET = contourgl

$(TARGET):	$(OBJS)
	$(CXX) -o $(TARGET) $(OBJS) $(LIBS)

all:	$(TARGET)

clean:
	rm -f $(OBJS) $(TARGET)
