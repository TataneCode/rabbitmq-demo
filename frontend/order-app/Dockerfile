FROM node:alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine AS final
COPY --from=build /app/dist/order-app/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
