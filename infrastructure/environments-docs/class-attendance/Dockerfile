FROM juancarloshidalgososa/doxygen:1.17.0

COPY ./infrastructure/docs/doxygen/Doxyfile ./Doxyfile
COPY ./infrastructure/docs/doxygen/doxygen-custom.css ./doxygen-extra/doxygen-custom.css
COPY ./infrastructure/docs/doxygen/doxygen-interface-highlight.js ./doxygen-extra/doxygen-interface-highlight.js
COPY ./infrastructure/docs/doxygen/header.html ./doxygen-extra/header.html

COPY ./apps/ClassAttendance/Backend/ ./

RUN sed -i 's|^PROJECT_NAME .*|PROJECT_NAME           = "DAMA Class Attendance Backend Documentation"|' Doxyfile
