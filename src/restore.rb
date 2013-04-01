#!/usr/bin/ruby

Dir["*/"].map do |dir|
    puts "Restore packages for '#{dir}'"
    Kernel::system "mono .nuget/NuGet.exe install #{dir}/packages.config -o 'packages'" if File::exists?("#{dir}/packages.config")
end
