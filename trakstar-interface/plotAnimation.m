clear; clc; close all;
M = csvread('data.txt');

figure;
% Plot the first data point only
% To-Do: add the ability to plot multiple sensor data concurrently
plot1 = scatter3(M(1, 5), M(1, 4), M(1, 3));
xlim([(min(M(:,5))-1) (max(M(:,5))+1)]);
ylim([(min(M(:,4))-1) (max(M(:,4))+1)]);
zlim([(min(M(:,3))-1) (max(M(:,3))+1)]);

i = 2;
for k = 2:size(M,1) 
     plot1.XData = M(i:k, 5); 
     plot1.YData = M(i:k, 4);  
     plot1.ZData = M(i:k, 3); 
     
     if k > 100 
         i = i + 1;
     end
     % pause 2/10 second: 
     pause(0.01)
end