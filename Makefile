PROJECT=WireMahiMahi.csproj
CONFIG=Release
FRAMEWORK=net8.0 # This is already a cross-platform target
RID=linux-x64 # Runtime identifier for Linux x64
PUBLISH_DIR=. # Directory where the published app will be placed

all: publish


build:
	@dotnet build -c $(CONFIG) -f $(FRAMEWORK) $(PROJECT)

publish: build
	@dotnet publish $(PROJECT) -c $(CONFIG) -r $(RID) --self-contained true -p:PublishSingleFile=true -o $(PUBLISH_DIR)

clean:
	@dotnet clean -c $(CONFIG) -f $(FRAMEWORK) $(PROJECT)

run: 
	@$(PUBLISH_DIR)/IPK

.PHONY: all build publish clean restore run